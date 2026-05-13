using IPop.Modules.Chat.Domain;

namespace IPop.Modules.Chat.Application.Messages;

public sealed record ChatMessageDto(
    Guid Id,
    Guid ConversationId,
    Guid SenderUserId,
    Guid RecipientUserId,
    Guid ClientMessageId,
    string Body,
    string BodyPlain,
    MessageType MessageType,
    DateTimeOffset SentAt,
    DateTimeOffset? DeliveredAt,
    DateTimeOffset? ReadAt,
    IReadOnlyCollection<AttachmentDto> Attachments);

public sealed record AttachmentDto(
    Guid Id,
    string OriginalName,
    long SizeBytes,
    string ContentType,
    string DownloadUrl);
