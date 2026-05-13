namespace IPop.Modules.Chat.Domain;

public sealed class ImAttachment
{
    private ImAttachment() { }

    public Guid Id { get; private set; }
    public Guid MessageId { get; private set; }
    public Guid UploaderUserId { get; private set; }
    public string OriginalName { get; private set; } = string.Empty;
    public string StoragePath { get; private set; } = string.Empty;
    public long SizeBytes { get; private set; }
    public string ContentType { get; private set; } = string.Empty;
    public DateTimeOffset UploadedAt { get; private set; }

    public static ImAttachment Create(Guid messageId, Guid uploaderUserId, string originalName, string storagePath, long sizeBytes, string contentType, DateTimeOffset now)
    {
        if (messageId == Guid.Empty) throw new ArgumentException("Message id is required.", nameof(messageId));
        if (uploaderUserId == Guid.Empty) throw new ArgumentException("Uploader user id is required.", nameof(uploaderUserId));
        if (string.IsNullOrWhiteSpace(originalName) || originalName.Contains("..") || originalName.Contains('/') || originalName.Contains('\\'))
            throw new ArgumentException("Invalid file name.", nameof(originalName));
        if (string.IsNullOrWhiteSpace(storagePath)) throw new ArgumentException("Storage path is required.", nameof(storagePath));
        if (sizeBytes <= 0) throw new ArgumentException("File size must be positive.", nameof(sizeBytes));
        if (string.IsNullOrWhiteSpace(contentType)) throw new ArgumentException("Content type is required.", nameof(contentType));

        return new ImAttachment
        {
            Id = Guid.NewGuid(),
            MessageId = messageId,
            UploaderUserId = uploaderUserId,
            OriginalName = originalName.Trim(),
            StoragePath = storagePath,
            SizeBytes = sizeBytes,
            ContentType = contentType.Trim(),
            UploadedAt = now
        };
    }
}
