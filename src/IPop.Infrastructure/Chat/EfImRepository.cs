using Microsoft.EntityFrameworkCore;
using IPop.Infrastructure.Persistence;
using IPop.Modules.Chat.Application.Abstractions;
using IPop.Modules.Chat.Application.Im;
using IPop.Modules.Chat.Domain;

namespace IPop.Infrastructure.Chat;

public sealed class EfImRepository(AppDbContext db) : IImRepository
{
    public async Task<IReadOnlyList<ImMessageSummary>> ListAsync(Guid currentUserId, ImFolder folder, CancellationToken cancellationToken)
    {
        var rows = await (
            from r in db.ImRecipients.AsNoTracking()
            join m in db.ImMessages.AsNoTracking() on r.MessageId equals m.Id
            join u in db.Users.AsNoTracking() on m.SenderUserId equals u.Id
            where r.UserId == currentUserId && r.Folder == folder
            orderby m.SentAt descending
            select new
            {
                m.Id,
                m.Subject,
                m.BodyPlain,
                m.SenderUserId,
                SenderName = u.DisplayName,
                r.Folder,
                r.IsRead,
                m.SentAt
            })
            .ToListAsync(cancellationToken);

        var messageIds = rows.Select(x => x.Id).ToList();
        var attachmentCounts = await db.ImAttachments.AsNoTracking()
            .Where(a => messageIds.Contains(a.MessageId))
            .GroupBy(a => a.MessageId)
            .Select(g => new { MessageId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.MessageId, x => x.Count, cancellationToken);
        var replyCounts = await db.ImMessages.AsNoTracking()
            .Where(m => m.ParentMessageId != null && messageIds.Contains(m.ParentMessageId.Value))
            .GroupBy(m => m.ParentMessageId!.Value)
            .Select(g => new { ParentId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ParentId, x => x.Count, cancellationToken);

        return rows.Select(x => new ImMessageSummary(
            x.Id, x.Subject, x.BodyPlain, x.SenderUserId, x.SenderName,
            x.Folder, x.IsRead, x.SentAt,
            attachmentCounts.GetValueOrDefault(x.Id),
            replyCounts.GetValueOrDefault(x.Id))).ToList();
    }

    public async Task<ImMessageDetail?> GetDetailAsync(Guid currentUserId, Guid messageId, CancellationToken cancellationToken)
    {
        var hasAccess = await db.ImRecipients.AsNoTracking()
            .AnyAsync(r => r.MessageId == messageId && r.UserId == currentUserId, cancellationToken);
        if (!hasAccess) return null;

        var message = await db.ImMessages.AsNoTracking()
            .Include(m => m.Recipients)
            .Include(m => m.Attachments)
            .SingleOrDefaultAsync(m => m.Id == messageId, cancellationToken);
        if (message is null) return null;

        var senderName = await db.Users.AsNoTracking()
            .Where(u => u.Id == message.SenderUserId)
            .Select(u => u.DisplayName)
            .SingleAsync(cancellationToken);

        var recipientUserIds = message.Recipients.Select(r => r.UserId).ToList();
        var userNames = await db.Users.AsNoTracking()
            .Where(u => recipientUserIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.DisplayName, cancellationToken);

        var recipientDtos = message.Recipients
            .Select(r => new ImRecipientDto(
                r.UserId,
                userNames.GetValueOrDefault(r.UserId, r.UserId.ToString()),
                r.Folder, r.IsRead, r.ReadAt))
            .ToList();

        var attachmentDtos = message.Attachments
            .OrderBy(a => a.UploadedAt)
            .Select(a => new ImAttachmentDto(a.Id, a.OriginalName, a.SizeBytes, a.ContentType, $"/api/im/attachments/{a.Id}"))
            .ToList();

        var threadReplies = await ListThreadRepliesAsync(messageId, currentUserId, cancellationToken);

        return new ImMessageDetail(
            message.Id, message.Subject, message.Body, message.BodyPlain,
            message.SenderUserId, senderName,
            recipientDtos, message.ParentMessageId, message.MessageType,
            message.SentAt, attachmentDtos, threadReplies);
    }

    public async Task<ImMessageSummary> SendAsync(Guid senderUserId, IReadOnlyList<Guid> recipientUserIds, string subject, string body, Guid? parentMessageId, ImMessageType messageType, CancellationToken cancellationToken)
    {
        var activeRecipients = await db.Users.AsNoTracking()
            .Where(u => recipientUserIds.Contains(u.Id) && u.IsActive)
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        var disabledCount = recipientUserIds.Distinct().Count(id => id != senderUserId && !activeRecipients.Contains(id));
        if (disabledCount > 0)
            throw new InvalidOperationException("One or more recipients were not found or are disabled.");

        var now = DateTimeOffset.UtcNow;
        var message = ImMessage.Create(subject, body, body, senderUserId, recipientUserIds, parentMessageId, messageType, now);
        await db.ImMessages.AddAsync(message, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        var senderName = await db.Users.AsNoTracking()
            .Where(u => u.Id == senderUserId)
            .Select(u => u.DisplayName)
            .SingleAsync(cancellationToken);

        return new ImMessageSummary(message.Id, message.Subject, message.BodyPlain, message.SenderUserId, senderName, ImFolder.Sent, true, message.SentAt, 0, 0);
    }

    public async Task MarkReadAsync(Guid currentUserId, Guid messageId, DateTimeOffset readAt, CancellationToken cancellationToken)
    {
        var recipient = await db.ImRecipients
            .SingleOrDefaultAsync(r => r.MessageId == messageId && r.UserId == currentUserId, cancellationToken);
        if (recipient is null) return;
        recipient.MarkRead(readAt);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid currentUserId, IReadOnlyList<Guid> messageIds, CancellationToken cancellationToken)
    {
        var recipients = await db.ImRecipients
            .Where(r => messageIds.Contains(r.MessageId) && r.UserId == currentUserId)
            .ToListAsync(cancellationToken);
        foreach (var r in recipients)
            r.MoveToDeleted();
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> GetUnreadCountAsync(Guid currentUserId, CancellationToken cancellationToken)
    {
        return await db.ImRecipients.AsNoTracking()
            .CountAsync(r => r.UserId == currentUserId && r.Folder == ImFolder.Inbox && !r.IsRead, cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> GetRecipientUserIdsAsync(Guid messageId, CancellationToken cancellationToken)
    {
        return await db.ImRecipients.AsNoTracking()
            .Where(r => r.MessageId == messageId && r.Folder == ImFolder.Inbox)
            .Select(r => r.UserId)
            .ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<ImMessageSummary>> ListThreadRepliesAsync(Guid parentMessageId, Guid currentUserId, CancellationToken cancellationToken)
    {
        var replies = await (
            from m in db.ImMessages.AsNoTracking()
            join r in db.ImRecipients.AsNoTracking() on m.Id equals r.MessageId
            join u in db.Users.AsNoTracking() on m.SenderUserId equals u.Id
            where m.ParentMessageId == parentMessageId && r.UserId == currentUserId
            orderby m.SentAt
            select new ImMessageSummary(m.Id, m.Subject, m.BodyPlain, m.SenderUserId, u.DisplayName, r.Folder, r.IsRead, m.SentAt, 0, 0))
            .ToListAsync(cancellationToken);
        return replies;
    }
}
