using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;
using IPop.Infrastructure.Chat;
using IPop.Modules.Chat.Application.Attachments;

namespace IPop.UnitTests.Modules.Chat;

public sealed class LocalFileStorageTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    [Fact]
    public async Task StoreAsync_WritesFileAndOpenRead_ReturnsContent()
    {
        var storage = CreateStorage();
        await using var content = new MemoryStream(Encoding.UTF8.GetBytes("hello"));

        var stored = await storage.StoreAsync(content, "hello.txt", "text/plain", CancellationToken.None);

        stored.SizeBytes.Should().Be(5);
        await using var stream = await storage.OpenReadAsync(stored.StoragePath, CancellationToken.None);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var actual = await reader.ReadToEndAsync(CancellationToken.None);
        actual.Should().Be("hello");
    }

    [Fact]
    public async Task StoreAsync_SanitizesPathTraversalFileName()
    {
        var storage = CreateStorage();
        await using var content = new MemoryStream(Encoding.UTF8.GetBytes("evil"));

        var stored = await storage.StoreAsync(content, "../evil.txt", "text/plain", CancellationToken.None);

        stored.StoragePath.Should().NotContain("..");
        stored.StoragePath.Should().NotContain("/evil");
        stored.StoragePath.Should().NotContain("\\");
    }

    [Fact]
    public async Task OpenReadAsync_PathTraversal_ThrowsUnauthorizedAccessException()
    {
        var storage = CreateStorage();

        var act = async () => await storage.OpenReadAsync("../evil.txt", CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    public void Dispose()
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, true);
        }
    }

    private LocalFileStorage CreateStorage()
    {
        return new LocalFileStorage(Options.Create(new FileStorageOptions { Root = root }));
    }
}
