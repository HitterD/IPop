using IPop.Modules.Chat.Application.Conversations;
using IPop.Modules.Chat.Application.Messages;
using IPop.Modules.Chat.Domain;

namespace IPop.Modules.Chat.Application.Abstractions;

public interface IChatRepository
{
    Task<IReadOnlyList<ConversationSummary>> ListRecentAsync(Guid currentUserId, CancellationToken cancellationToken);
    Task<IReadOnlyList<ChatMessageDto>> ListMessagesAsync(Guid currentUserId, Guid otherUserId, CancellationToken cancellationToken);
    Task<ChatMessageDto> SendDirectMessageAsync(Guid senderUserId, Guid recipientUserId, Guid clientMessageId, string body, IReadOnlyCollection<Guid> attachmentIds, CancellationToken cancellationToken);
    Task MarkDeliveredAsync(Guid messageId, DateTimeOffset deliveredAt, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Guid>> MarkReadAsync(Guid currentUserId, IReadOnlyCollection<Guid> messageIds, DateTimeOffset readAt, CancellationToken cancellationToken);
    Task<IReadOnlyList<ChatMessageDto>> MarkUndeliveredMessagesDeliveredAsync(Guid recipientUserId, DateTimeOffset deliveredAt, CancellationToken cancellationToken);
    Task<AttachmentDto> StageAttachmentAsync(Guid senderUserId, string originalName, string storagePath, long sizeBytes, string contentType, CancellationToken cancellationToken);
    Task<DirectAttachment?> FindAttachmentForDownloadAsync(Guid attachmentId, Guid currentUserId, CancellationToken cancellationToken);
    Task<IReadOnlyList<AttachmentDto>> ListAttachmentsAsync(Guid currentUserId, Guid otherUserId, string kind, CancellationToken cancellationToken);
}
