namespace IPop.Modules.Chat.Domain;

public sealed class DirectAttachment
{
    private DirectAttachment() { }
    public Guid Id { get; private set; }
    public Guid? MessageId { get; private set; }
    public Guid SenderUserId { get; private set; }
    public string OriginalName { get; private set; } = string.Empty;
    public string StoragePath { get; private set; } = string.Empty;
    public long SizeBytes { get; private set; }
    public string ContentType { get; private set; } = string.Empty;
    public DateTimeOffset UploadedAt { get; private set; }
    public AttachmentScanStatus ScanStatus { get; private set; }
    public DirectMessage? Message { get; private set; }

    public static DirectAttachment Create(Guid senderUserId, string originalName, string storagePath, long sizeBytes, string contentType, DateTimeOffset now)
    {
        if (senderUserId == Guid.Empty) throw new ArgumentException("Sender user id is required.", nameof(senderUserId));
        if (string.IsNullOrWhiteSpace(originalName) || originalName.Contains("..") || originalName.Contains('/') || originalName.Contains('\\')) throw new ArgumentException("Invalid file name.", nameof(originalName));
        if (string.IsNullOrWhiteSpace(storagePath)) throw new ArgumentException("Storage path is required.", nameof(storagePath));
        if (sizeBytes <= 0) throw new ArgumentException("File size must be positive.", nameof(sizeBytes));
        if (string.IsNullOrWhiteSpace(contentType)) throw new ArgumentException("Content type is required.", nameof(contentType));
        return new DirectAttachment { Id = Guid.NewGuid(), SenderUserId = senderUserId, OriginalName = originalName.Trim(), StoragePath = storagePath, SizeBytes = sizeBytes, ContentType = contentType.Trim(), UploadedAt = now, ScanStatus = AttachmentScanStatus.Pending };
    }

    public void AttachToMessage(Guid messageId)
    {
        if (MessageId is not null) return;
        MessageId = messageId;
    }
}
