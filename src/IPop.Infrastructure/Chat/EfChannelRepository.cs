using Microsoft.EntityFrameworkCore;
using IPop.Infrastructure.Persistence;
using IPop.Modules.Chat.Application.Abstractions;
using IPop.Modules.Chat.Application.Channels;
using IPop.Modules.Chat.Domain;

namespace IPop.Infrastructure.Chat;

public sealed class EfChannelRepository(AppDbContext db) : IChannelRepository
{
    public async Task<IReadOnlyList<ChannelSummary>> ListAsync(Guid currentUserId, CancellationToken cancellationToken)
    {
        var channels = await db.Channels
            .AsNoTracking()
            .Include(c => c.Members)
            .OrderByDescending(c => c.LastMessageAt)
            .ToListAsync(cancellationToken);

        var visible = channels
            .Where(c => c.Type == ChannelType.Public || c.Members.Any(m => m.UserId == currentUserId))
            .ToList();

        var channelIds = visible.Select(c => c.Id).ToList();
        var previews = await db.ChannelMessages
            .AsNoTracking()
            .Where(m => channelIds.Contains(m.ChannelId))
            .GroupBy(m => m.ChannelId)
            .Select(g => new
            {
                ChannelId = g.Key,
                LastBody = g.OrderByDescending(m => m.SentAt).Select(m => m.BodyPlain).FirstOrDefault() ?? string.Empty,
                LastAt = g.Max(m => m.SentAt)
            })
            .ToListAsync(cancellationToken);
        var previewMap = previews.ToDictionary(x => x.ChannelId, x => (x.LastBody, x.LastAt));

        return visible
            .Select(c =>
            {
                var preview = previewMap.TryGetValue(c.Id, out var p) ? p.LastBody : string.Empty;
                var lastAt = previewMap.TryGetValue(c.Id, out var p2) ? p2.LastAt : c.LastMessageAt;
                return new ChannelSummary(
                    c.Id,
                    c.Name,
                    c.Description,
                    (ChannelTypeDto)(int)c.Type,
                    c.Members.Count,
                    preview,
                    lastAt,
                    c.Members.Any(m => m.UserId == currentUserId));
            })
            .ToList();
    }

    public async Task<ChannelSummary?> GetAsync(Guid channelId, Guid currentUserId, CancellationToken cancellationToken)
    {
        var channel = await db.Channels
            .AsNoTracking()
            .Include(c => c.Members)
            .SingleOrDefaultAsync(c => c.Id == channelId, cancellationToken);
        if (channel is null)
        {
            return null;
        }
        if (channel.Type == ChannelType.Private && !channel.Members.Any(m => m.UserId == currentUserId))
        {
            return null;
        }
        var lastMessage = await db.ChannelMessages
            .AsNoTracking()
            .Where(m => m.ChannelId == channelId)
            .OrderByDescending(m => m.SentAt)
            .Select(m => new { m.BodyPlain, m.SentAt })
            .FirstOrDefaultAsync(cancellationToken);
        return new ChannelSummary(
            channel.Id,
            channel.Name,
            channel.Description,
            (ChannelTypeDto)(int)channel.Type,
            channel.Members.Count,
            lastMessage?.BodyPlain ?? string.Empty,
            lastMessage?.SentAt ?? channel.LastMessageAt,
            channel.Members.Any(m => m.UserId == currentUserId));
    }

    public async Task<ChannelSummary> CreateAsync(Guid creatorUserId, string name, string? description, ChannelTypeDto type, CancellationToken cancellationToken)
    {
        var creatorExists = await db.Users.AsNoTracking().AnyAsync(u => u.Id == creatorUserId && u.IsActive, cancellationToken);
        if (!creatorExists)
        {
            throw new InvalidOperationException("Creator user was not found or is disabled.");
        }
        var trimmedKey = name.Trim().ToLowerInvariant();
        var duplicate = await db.Channels.AsNoTracking().AnyAsync(c => c.NameKey == trimmedKey, cancellationToken);
        if (duplicate)
        {
            throw new InvalidOperationException("A channel with the same name already exists.");
        }
        var now = DateTimeOffset.UtcNow;
        var channel = Channel.Create(name, description, (ChannelType)(int)type, creatorUserId, now);
        await db.Channels.AddAsync(channel, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return new ChannelSummary(
            channel.Id,
            channel.Name,
            channel.Description,
            type,
            channel.Members.Count,
            string.Empty,
            channel.LastMessageAt,
            true);
    }

    public async Task<ChannelMemberDto> JoinAsync(Guid channelId, Guid userId, CancellationToken cancellationToken)
    {
        var channel = await db.Channels.AsNoTracking().SingleOrDefaultAsync(c => c.Id == channelId, cancellationToken)
            ?? throw new InvalidOperationException("Channel was not found.");
        if (channel.Type == ChannelType.Private)
        {
            throw new InvalidOperationException("Private channel requires an invitation.");
        }
        var userExists = await db.Users.AsNoTracking().AnyAsync(u => u.Id == userId && u.IsActive, cancellationToken);
        if (!userExists)
        {
            throw new InvalidOperationException("User was not found or is disabled.");
        }
        var existing = await db.ChannelMembers
            .AsNoTracking()
            .SingleOrDefaultAsync(m => m.ChannelId == channelId && m.UserId == userId, cancellationToken);
        if (existing is not null)
        {
            return await ToMemberDtoAsync(existing, cancellationToken);
        }
        var count = await db.ChannelMembers.CountAsync(m => m.ChannelId == channelId, cancellationToken);
        if (count >= Channel.MemberCapacity)
        {
            throw new InvalidOperationException("Channel member capacity exceeded.");
        }
        var member = ChannelMember.Create(channelId, userId, ChannelMemberRole.Member, DateTimeOffset.UtcNow);
        await db.ChannelMembers.AddAsync(member, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return await ToMemberDtoAsync(member, cancellationToken);
    }

    public async Task LeaveAsync(Guid channelId, Guid userId, CancellationToken cancellationToken)
    {
        var channel = await db.Channels.AsNoTracking().SingleOrDefaultAsync(c => c.Id == channelId, cancellationToken);
        if (channel is null)
        {
            return;
        }
        var member = await db.ChannelMembers.SingleOrDefaultAsync(m => m.ChannelId == channelId && m.UserId == userId, cancellationToken);
        if (member is null)
        {
            return;
        }
        if (member.UserId == channel.CreatedByUserId)
        {
            var adminCount = await db.ChannelMembers.CountAsync(m => m.ChannelId == channelId && m.Role == ChannelMemberRole.Admin, cancellationToken);
            if (adminCount <= 1)
            {
                throw new InvalidOperationException("Cannot remove the last admin of a channel.");
            }
        }
        db.ChannelMembers.Remove(member);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ChannelMemberDto>> ListMembersAsync(Guid channelId, Guid currentUserId, CancellationToken cancellationToken)
    {
        var isMember = await db.ChannelMembers.AsNoTracking().AnyAsync(m => m.ChannelId == channelId && m.UserId == currentUserId, cancellationToken);
        var channel = await db.Channels.AsNoTracking().SingleOrDefaultAsync(c => c.Id == channelId, cancellationToken);
        if (channel is null)
        {
            return Array.Empty<ChannelMemberDto>();
        }
        if (channel.Type == ChannelType.Private && !isMember)
        {
            return Array.Empty<ChannelMemberDto>();
        }

        var rows = await (
            from m in db.ChannelMembers.AsNoTracking()
            join u in db.Users.AsNoTracking() on m.UserId equals u.Id
            where m.ChannelId == channelId
            orderby u.DisplayName
            select new ChannelMemberDto(m.UserId, u.DisplayName, m.Role.ToString(), m.JoinedAt))
            .ToListAsync(cancellationToken);
        return rows;
    }

    public async Task<IReadOnlyList<ChannelMessageDto>> ListMessagesAsync(Guid channelId, Guid currentUserId, CancellationToken cancellationToken)
    {
        await EnsureMembershipAsync(channelId, currentUserId, cancellationToken);
        return await (
            from m in db.ChannelMessages.AsNoTracking()
            join u in db.Users.AsNoTracking() on m.SenderUserId equals u.Id
            where m.ChannelId == channelId
            orderby m.SentAt
            select new ChannelMessageDto(
                m.Id,
                m.ChannelId,
                m.SenderUserId,
                u.DisplayName,
                m.ClientMessageId,
                m.Body,
                m.BodyPlain,
                m.MessageType,
                m.SentAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<ChannelMessageDto> SendMessageAsync(Guid channelId, Guid senderUserId, Guid clientMessageId, string body, CancellationToken cancellationToken)
    {
        var existing = await db.ChannelMessages
            .AsNoTracking()
            .Where(x => x.ChannelId == channelId && x.SenderUserId == senderUserId && x.ClientMessageId == clientMessageId)
            .Select(x => new { x.Id, x.ChannelId, x.SenderUserId, x.ClientMessageId, x.Body, x.BodyPlain, x.MessageType, x.SentAt })
            .SingleOrDefaultAsync(cancellationToken);
        if (existing is not null)
        {
            var existingSender = await db.Users.AsNoTracking().Where(u => u.Id == existing.SenderUserId).Select(u => u.DisplayName).SingleAsync(cancellationToken);
            return new ChannelMessageDto(existing.Id, existing.ChannelId, existing.SenderUserId, existingSender, existing.ClientMessageId, existing.Body, existing.BodyPlain, existing.MessageType, existing.SentAt);
        }

        await EnsureMembershipAsync(channelId, senderUserId, cancellationToken);
        var channel = await db.Channels.SingleAsync(c => c.Id == channelId, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var message = ChannelMessage.Create(channelId, senderUserId, clientMessageId, body, Truncate(body, 400), MessageType.Text, now);
        channel.Touch(now);
        await db.ChannelMessages.AddAsync(message, cancellationToken);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();
            throw;
        }
        var senderName = await db.Users.AsNoTracking().Where(u => u.Id == senderUserId).Select(u => u.DisplayName).SingleAsync(cancellationToken);
        return new ChannelMessageDto(message.Id, message.ChannelId, message.SenderUserId, senderName, message.ClientMessageId, message.Body, message.BodyPlain, message.MessageType, message.SentAt);
    }

    public async Task<IReadOnlyCollection<Guid>> GetMemberUserIdsAsync(Guid channelId, CancellationToken cancellationToken)
    {
        return await db.ChannelMembers
            .AsNoTracking()
            .Where(m => m.ChannelId == channelId)
            .Select(m => m.UserId)
            .ToListAsync(cancellationToken);
    }

    private async Task EnsureMembershipAsync(Guid channelId, Guid userId, CancellationToken cancellationToken)
    {
        var isMember = await db.ChannelMembers.AsNoTracking().AnyAsync(m => m.ChannelId == channelId && m.UserId == userId, cancellationToken);
        if (!isMember)
        {
            throw new InvalidOperationException("User is not a member of the channel.");
        }
    }

    private async Task<ChannelMemberDto> ToMemberDtoAsync(ChannelMember member, CancellationToken cancellationToken)
    {
        var displayName = await db.Users.AsNoTracking().Where(u => u.Id == member.UserId).Select(u => u.DisplayName).SingleAsync(cancellationToken);
        return new ChannelMemberDto(member.UserId, displayName, member.Role.ToString(), member.JoinedAt);
    }

    private static string Truncate(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength) return text;
        return text[..maxLength];
    }
}
