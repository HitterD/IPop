using Microsoft.EntityFrameworkCore;
using IPop.Infrastructure.Persistence;
using IPop.Modules.Chat.Application.Abstractions;
using IPop.Modules.Chat.Application.Conversations;
using IPop.Modules.Chat.Application.Messages;
using IPop.Modules.Chat.Domain;

namespace IPop.Infrastructure.Chat;

public sealed class EfChatRepository(AppDbContext db) : IChatRepository
{
    public async Task<IReadOnlyList<ConversationSummary>> ListRecentAsync(Guid currentUserId, CancellationToken cancellationToken)
    {
        var rows = await db.DirectConversations
            .AsNoTracking()
            .Where(x => x.UserAId == currentUserId || x.UserBId == currentUserId)
            .OrderByDescending(x => x.LastMessageAt)
            .Select(x => new
            {
                OtherUserId = x.UserAId == currentUserId ? x.UserBId : x.UserAId,
                x.LastMessageAt,
                LastMessagePreview = db.DirectMessages
                    .Where(m => m.ConversationId == x.Id)
                    .OrderByDescending(m => m.SentAt)
                    .Select(m => m.Body)
                    .FirstOrDefault() ?? string.Empty
            })
            .ToListAsync(cancellationToken);

        var userIds = rows.Select(x => x.OtherUserId).Distinct().ToList();
        var names = await db.Users
            .AsNoTracking()
            .Where(x => userIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.DisplayName, cancellationToken);

        return rows.Select(x => new ConversationSummary(
            x.OtherUserId,
            names.TryGetValue(x.OtherUserId, out var name) ? name : x.OtherUserId.ToString(),
            x.LastMessagePreview,
            x.LastMessageAt)).ToList();
    }

    public async Task<IReadOnlyList<ChatMessageDto>> ListMessagesAsync(Guid currentUserId, Guid otherUserId, CancellationToken cancellationToken)
    {
        var (userA, userB) = NormalizePair(currentUserId, otherUserId);
        var conversation = await db.DirectConversations
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.UserAId == userA && x.UserBId == userB, cancellationToken);
        if (conversation is null)
        {
            return Array.Empty<ChatMessageDto>();
        }

        return await db.DirectMessages
            .AsNoTracking()
            .Where(x => x.ConversationId == conversation.Id)
            .OrderBy(x => x.SentAt)
            .Select(x => new ChatMessageDto(
                x.Id,
                x.ConversationId,
                x.SenderUserId,
                x.RecipientUserId,
                x.ClientMessageId,
                x.Body,
                x.BodyPlain,
                x.MessageType,
                x.SentAt,
                x.DeliveredAt,
                x.ReadAt,
                x.Attachments
                    .OrderBy(a => a.UploadedAt)
                    .Select(a => ToAttachmentDto(a))
                    .ToList()))
            .ToListAsync(cancellationToken);
    }

    public async Task<ChatMessageDto> SendDirectMessageAsync(Guid senderUserId, Guid recipientUserId, Guid clientMessageId, string body, IReadOnlyCollection<Guid> attachmentIds, CancellationToken cancellationToken)
    {
        var existing = await db.DirectMessages
            .AsNoTracking()
            .Where(x => x.SenderUserId == senderUserId && x.ClientMessageId == clientMessageId)
            .Select(x => new ChatMessageDto(x.Id, x.ConversationId, x.SenderUserId, x.RecipientUserId, x.ClientMessageId, x.Body, x.BodyPlain, x.MessageType, x.SentAt, x.DeliveredAt, x.ReadAt, Array.Empty<AttachmentDto>()))
            .SingleOrDefaultAsync(cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var recipientExists = await db.Users.AnyAsync(x => x.Id == recipientUserId && x.IsActive, cancellationToken);
        if (!recipientExists)
        {
            throw new InvalidOperationException("Recipient was not found or is disabled.");
        }

        var now = DateTimeOffset.UtcNow;
        var (userA, userB) = NormalizePair(senderUserId, recipientUserId);
        var conversation = await db.DirectConversations
            .SingleOrDefaultAsync(x => x.UserAId == userA && x.UserBId == userB, cancellationToken);
        if (conversation is null)
        {
            conversation = DirectConversation.Create(senderUserId, recipientUserId, now);
            await db.DirectConversations.AddAsync(conversation, cancellationToken);
        }
        else
        {
            conversation.Touch(now);
        }

        var message = DirectMessage.Create(conversation.Id, senderUserId, recipientUserId, clientMessageId, body, Truncate(body, 400), MessageType.Text, now);
        await db.DirectMessages.AddAsync(message, cancellationToken);

        if (attachmentIds.Count > 0)
        {
            var distinctIds = attachmentIds.Distinct().ToList();
            var staged = await db.DirectAttachments
                .Where(a => distinctIds.Contains(a.Id) && a.SenderUserId == senderUserId && a.MessageId == null)
                .ToListAsync(cancellationToken);
            foreach (var att in staged)
            {
                att.AttachToMessage(message.Id);
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        var attachmentDtos = await db.DirectAttachments
            .AsNoTracking()
            .Where(a => a.MessageId == message.Id)
            .OrderBy(a => a.UploadedAt)
            .Select(a => ToAttachmentDto(a))
            .ToListAsync(cancellationToken);

        return new ChatMessageDto(message.Id, message.ConversationId, message.SenderUserId, message.RecipientUserId, message.ClientMessageId, message.Body, message.BodyPlain, message.MessageType, message.SentAt, message.DeliveredAt, message.ReadAt, attachmentDtos);
    }

    public async Task MarkDeliveredAsync(Guid messageId, DateTimeOffset deliveredAt, CancellationToken cancellationToken)
    {
        var message = await db.DirectMessages.SingleOrDefaultAsync(x => x.Id == messageId, cancellationToken);
        if (message is null)
        {
            return;
        }

        message.MarkDelivered(deliveredAt);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Guid>> MarkReadAsync(Guid currentUserId, IReadOnlyCollection<Guid> messageIds, DateTimeOffset readAt, CancellationToken cancellationToken)
    {
        if (messageIds.Count == 0)
        {
            return Array.Empty<Guid>();
        }

        var distinctMessageIds = messageIds.Distinct().ToList();
        var messages = await db.DirectMessages
            .Where(x => distinctMessageIds.Contains(x.Id) && x.RecipientUserId == currentUserId)
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            message.MarkRead(readAt);
        }

        await db.SaveChangesAsync(cancellationToken);
        return messages.Select(x => x.SenderUserId).Distinct().ToList();
    }

    public async Task<IReadOnlyList<ChatMessageDto>> MarkUndeliveredMessagesDeliveredAsync(Guid recipientUserId, DateTimeOffset deliveredAt, CancellationToken cancellationToken)
    {
        var messages = await db.DirectMessages
            .Include(x => x.Attachments)
            .Where(x => x.RecipientUserId == recipientUserId && x.DeliveredAt == null)
            .OrderBy(x => x.SentAt)
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            message.MarkDelivered(deliveredAt);
        }

        await db.SaveChangesAsync(cancellationToken);
        return messages.Select(ToMessageDto).ToList();
    }

    public async Task<AttachmentDto> StageAttachmentAsync(Guid senderUserId, string originalName, string storagePath, long sizeBytes, string contentType, CancellationToken cancellationToken)
    {
        var attachment = DirectAttachment.Create(senderUserId, originalName, storagePath, sizeBytes, contentType, DateTimeOffset.UtcNow);
        await db.DirectAttachments.AddAsync(attachment, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return ToAttachmentDto(attachment);
    }

    public async Task<DirectAttachment?> FindAttachmentForDownloadAsync(Guid attachmentId, Guid currentUserId, CancellationToken cancellationToken)
    {
        return await db.DirectAttachments
            .AsNoTracking()
            .Include(x => x.Message)
            .Where(x => x.Id == attachmentId)
            .Where(x =>
                (x.SenderUserId == currentUserId && x.MessageId == null) ||
                (x.Message != null && (x.Message.SenderUserId == currentUserId || x.Message.RecipientUserId == currentUserId)))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AttachmentDto>> ListAttachmentsAsync(Guid currentUserId, Guid otherUserId, string kind, CancellationToken cancellationToken)
    {
        if (string.Equals(kind, "links", StringComparison.OrdinalIgnoreCase))
        {
            return Array.Empty<AttachmentDto>();
        }

        var (userA, userB) = NormalizePair(currentUserId, otherUserId);
        var conversation = await db.DirectConversations
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.UserAId == userA && x.UserBId == userB, cancellationToken);
        if (conversation is null)
        {
            return Array.Empty<AttachmentDto>();
        }

        var query = db.DirectAttachments
            .AsNoTracking()
            .Where(x => x.Message != null && x.Message.ConversationId == conversation.Id);

        query = string.Equals(kind, "photos", StringComparison.OrdinalIgnoreCase)
            ? query.Where(x => x.ContentType.StartsWith("image/"))
            : query.Where(x => !x.ContentType.StartsWith("image/"));

        return await query
            .OrderByDescending(x => x.UploadedAt)
            .Select(x => ToAttachmentDto(x))
            .ToListAsync(cancellationToken);
    }

    private static ChatMessageDto ToMessageDto(DirectMessage message)
    {
        return new ChatMessageDto(
            message.Id,
            message.ConversationId,
            message.SenderUserId,
            message.RecipientUserId,
            message.ClientMessageId,
            message.Body,
            message.BodyPlain,
            message.MessageType,
            message.SentAt,
            message.DeliveredAt,
            message.ReadAt,
            message.Attachments
                .OrderBy(a => a.UploadedAt)
                .Select(ToAttachmentDto)
                .ToList());
    }

    private static AttachmentDto ToAttachmentDto(DirectAttachment attachment)
    {
        return new AttachmentDto(attachment.Id, attachment.OriginalName, attachment.SizeBytes, attachment.ContentType, $"/api/chat/attachments/{attachment.Id}");
    }

    private static (Guid UserAId, Guid UserBId) NormalizePair(Guid firstUserId, Guid secondUserId)
    {
        return firstUserId.CompareTo(secondUserId) < 0 ? (firstUserId, secondUserId) : (secondUserId, firstUserId);
    }

    private static string Truncate(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength) return text;
        return text[..maxLength];
    }
}
