using Microsoft.Extensions.Options;
using IPop.Modules.Chat.Application.Attachments;

namespace IPop.Infrastructure.Chat;

public sealed class LocalFileStorage(IOptions<FileStorageOptions> options) : IFileStorage
{
    private readonly FileStorageOptions options = options.Value;

    public async Task<StoredFile> StoreAsync(Stream content, string suggestedName, string contentType, CancellationToken cancellationToken)
    {
        var safeName = SanitizeFileName(Path.GetFileName(suggestedName));
        var relativeDirectory = Path.Combine("chat", DateTimeOffset.UtcNow.ToString("yyyy"), DateTimeOffset.UtcNow.ToString("MM"));
        var directory = Path.Combine(options.Root, relativeDirectory);
        Directory.CreateDirectory(directory);

        var fileName = $"{Guid.NewGuid():N}-{safeName}";
        var finalPath = Path.Combine(directory, fileName);
        var tempPath = finalPath + ".tmp";

        await using (var output = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous))
        {
            await content.CopyToAsync(output, cancellationToken);
            await output.FlushAsync(cancellationToken);
        }

        File.Move(tempPath, finalPath);
        var relativePath = Path.Combine(relativeDirectory, fileName).Replace(Path.DirectorySeparatorChar, '/');
        var sizeBytes = new FileInfo(finalPath).Length;

        return new StoredFile(relativePath, sizeBytes);
    }

    public Task<Stream> OpenReadAsync(string storagePath, CancellationToken cancellationToken)
    {
        var root = Path.GetFullPath(options.Root);
        var fullPath = Path.GetFullPath(Path.Combine(root, storagePath));
        var normalizedRoot = root.EndsWith(Path.DirectorySeparatorChar) ? root : root + Path.DirectorySeparatorChar;

        if (!fullPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("Storage path is outside root.");
        }

        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous);
        return Task.FromResult(stream);
    }

    private static string SanitizeFileName(string fileName)
    {
        var name = string.IsNullOrWhiteSpace(fileName) ? "file" : fileName;
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new string(name.Select(x => invalid.Contains(x) ? '-' : x).ToArray());
        return sanitized.Trim('.') is { Length: > 0 } trimmed ? trimmed : "file";
    }
}
