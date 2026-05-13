namespace IPop.Modules.Chat.Application.Attachments;

public interface IFileStorage
{
    Task<StoredFile> StoreAsync(Stream content, string suggestedName, string contentType, CancellationToken cancellationToken);

    Task<Stream> OpenReadAsync(string storagePath, CancellationToken cancellationToken);
}

public sealed record StoredFile(string StoragePath, long SizeBytes);

public sealed class FileStorageOptions
{
    public string Root { get; init; } = "./uploads";

    public long MaxBytes { get; init; } = 26214400;

    public string[] AllowedMimeTypes { get; init; } = [];
}
