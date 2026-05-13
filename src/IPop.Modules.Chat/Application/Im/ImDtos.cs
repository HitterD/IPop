using IPop.Modules.Chat.Domain;

namespace IPop.Modules.Chat.Application.Im;

public sealed record ImMessageSummary(
    Guid Id,
    string Subject,
    string BodyPlain,
    Guid SenderUserId,
    string SenderDisplayName,
    ImFolder Folder,
    bool IsRead,
    DateTimeOffset SentAt,
    int AttachmentCount,
    int ReplyCount);

public sealed record ImMessageDetail(
    Guid Id,
    string Subject,
    string Body,
    string BodyPlain,
    Guid SenderUserId,
    string SenderDisplayName,
    IReadOnlyList<ImRecipientDto> Recipients,
    Guid? ParentMessageId,
    ImMessageType MessageType,
    DateTimeOffset SentAt,
    IReadOnlyList<ImAttachmentDto> Attachments,
    IReadOnlyList<ImMessageSummary> ThreadReplies);

public sealed record ImRecipientDto(
    Guid UserId,
    string DisplayName,
    ImFolder Folder,
    bool IsRead,
    DateTimeOffset? ReadAt);

public sealed record ImAttachmentDto(
    Guid Id,
    string OriginalName,
    long SizeBytes,
    string ContentType,
    string DownloadUrl);
