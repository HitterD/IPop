using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using IPop.Infrastructure.Chat;
using IPop.Infrastructure.Persistence;
using IPop.Modules.Auth.Domain;
using IPop.Modules.Chat.Domain;
using Xunit;

namespace IPop.UnitTests.Modules.Chat;

public sealed class EfChatRepositoryTests
{
    [Fact]
    public async Task SendDirectMessageAsync_FirstMessage_CreatesConversationAndMessage()
    {
        await using var db = CreateDb();
        var sender = User.Create("1001", "Alice", "IT", "Dev", "HQ", "hash");
        var recipient = User.Create("1002", "Bob", "IT", "Dev", "HQ", "hash");
        db.Users.AddRange(sender, recipient);
        await db.SaveChangesAsync();
        var repository = new EfChatRepository(db);
        var clientMessageId = Guid.NewGuid();

        var message = await repository.SendDirectMessageAsync(sender.Id, recipient.Id, clientMessageId, "hello", Array.Empty<Guid>(), default);

        message.Body.Should().Be("hello");
        (await db.DirectConversations.CountAsync()).Should().Be(1);
        (await db.DirectMessages.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task SendDirectMessageAsync_SameClientMessageId_ReturnsExistingMessage()
    {
        await using var db = CreateDb();
        var sender = User.Create("1001", "Alice", "IT", "Dev", "HQ", "hash");
        var recipient = User.Create("1002", "Bob", "IT", "Dev", "HQ", "hash");
        db.Users.AddRange(sender, recipient);
        await db.SaveChangesAsync();
        var repository = new EfChatRepository(db);
        var clientMessageId = Guid.NewGuid();

        var first = await repository.SendDirectMessageAsync(sender.Id, recipient.Id, clientMessageId, "hello", Array.Empty<Guid>(), default);
        var second = await repository.SendDirectMessageAsync(sender.Id, recipient.Id, clientMessageId, "hello retry", Array.Empty<Guid>(), default);

        second.Id.Should().Be(first.Id);
        second.Body.Should().Be("hello");
        (await db.DirectMessages.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task ListMessagesAsync_ReturnsOnlyConversationMessages()
    {
        await using var db = CreateDb();
        var alice = User.Create("1001", "Alice", "IT", "Dev", "HQ", "hash");
        var bob = User.Create("1002", "Bob", "IT", "Dev", "HQ", "hash");
        var carol = User.Create("1003", "Carol", "IT", "Dev", "HQ", "hash");
        db.Users.AddRange(alice, bob, carol);
        await db.SaveChangesAsync();
        var repository = new EfChatRepository(db);
        await repository.SendDirectMessageAsync(alice.Id, bob.Id, Guid.NewGuid(), "to bob", Array.Empty<Guid>(), default);
        await repository.SendDirectMessageAsync(alice.Id, carol.Id, Guid.NewGuid(), "to carol", Array.Empty<Guid>(), default);

        var messages = await repository.ListMessagesAsync(alice.Id, bob.Id, default);

        messages.Should().ContainSingle();
        messages[0].Body.Should().Be("to bob");
    }

    [Fact]
    public async Task SendDirectMessageAsync_DisabledRecipient_Throws()
    {
        await using var db = CreateDb();
        var sender = User.Create("1001", "Alice", "IT", "Dev", "HQ", "hash");
        var recipient = User.Create("1002", "Bob", "IT", "Dev", "HQ", "hash");
        recipient.Disable("admin");
        db.Users.AddRange(sender, recipient);
        await db.SaveChangesAsync();
        var repository = new EfChatRepository(db);

        var act = async () => await repository.SendDirectMessageAsync(sender.Id, recipient.Id, Guid.NewGuid(), "hi", Array.Empty<Guid>(), default);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ListRecentAsync_SortedByLastMessageAtDescending()
    {
        await using var db = CreateDb();
        var me = User.Create("1001", "Alice", "IT", "Dev", "HQ", "hash");
        var bob = User.Create("1002", "Bob", "IT", "Dev", "HQ", "hash");
        var carol = User.Create("1003", "Carol", "IT", "Dev", "HQ", "hash");
        db.Users.AddRange(me, bob, carol);
        await db.SaveChangesAsync();
        var repository = new EfChatRepository(db);

        await repository.SendDirectMessageAsync(me.Id, bob.Id, Guid.NewGuid(), "old bob", Array.Empty<Guid>(), default);
        await Task.Delay(10);
        await repository.SendDirectMessageAsync(me.Id, carol.Id, Guid.NewGuid(), "new carol", Array.Empty<Guid>(), default);

        var recent = await repository.ListRecentAsync(me.Id, default);

        recent.Should().HaveCount(2);
        recent[0].OtherUserId.Should().Be(carol.Id);
        recent[0].OtherDisplayName.Should().Be("Carol");
        recent[0].LastMessagePreview.Should().Be("new carol");
    }

    [Fact]
    public async Task StageAttachmentAsync_PersistsMetadataAndReturnsDownloadUrl()
    {
        await using var db = CreateDb();
        var sender = User.Create("1001", "Alice", "IT", "Dev", "HQ", "hash");
        db.Users.Add(sender);
        await db.SaveChangesAsync();
        var repository = new EfChatRepository(db);

        var attachment = await repository.StageAttachmentAsync(sender.Id, "photo.png", "chat/photo.png", 123, "image/png", default);

        attachment.DownloadUrl.Should().Be($"/api/chat/attachments/{attachment.Id}");
        var entity = await db.DirectAttachments.SingleAsync();
        entity.SenderUserId.Should().Be(sender.Id);
        entity.OriginalName.Should().Be("photo.png");
        entity.StoragePath.Should().Be("chat/photo.png");
        entity.SizeBytes.Should().Be(123);
        entity.ContentType.Should().Be("image/png");
        entity.MessageId.Should().BeNull();
    }

    [Fact]
    public async Task FindAttachmentForDownloadAsync_ReturnsStagedAttachmentForSender()
    {
        await using var db = CreateDb();
        var sender = User.Create("1001", "Alice", "IT", "Dev", "HQ", "hash");
        db.Users.Add(sender);
        await db.SaveChangesAsync();
        var repository = new EfChatRepository(db);
        var attachment = await repository.StageAttachmentAsync(sender.Id, "photo.png", "chat/photo.png", 123, "image/png", default);

        var found = await repository.FindAttachmentForDownloadAsync(attachment.Id, sender.Id, default);

        found.Should().NotBeNull();
        found!.StoragePath.Should().Be("chat/photo.png");
    }

    [Fact]
    public async Task FindAttachmentForDownloadAsync_ReturnsNullForOtherUserWhenStaged()
    {
        await using var db = CreateDb();
        var sender = User.Create("1001", "Alice", "IT", "Dev", "HQ", "hash");
        var other = User.Create("1002", "Bob", "IT", "Dev", "HQ", "hash");
        db.Users.AddRange(sender, other);
        await db.SaveChangesAsync();
        var repository = new EfChatRepository(db);
        var attachment = await repository.StageAttachmentAsync(sender.Id, "photo.png", "chat/photo.png", 123, "image/png", default);

        var found = await repository.FindAttachmentForDownloadAsync(attachment.Id, other.Id, default);

        found.Should().BeNull();
    }

    [Fact]
    public async Task ListAttachmentsAsync_ReturnsImageAttachmentsForConversation()
    {
        await using var db = CreateDb();
        var alice = User.Create("1001", "Alice", "IT", "Dev", "HQ", "hash");
        var bob = User.Create("1002", "Bob", "IT", "Dev", "HQ", "hash");
        var carol = User.Create("1003", "Carol", "IT", "Dev", "HQ", "hash");
        db.Users.AddRange(alice, bob, carol);
        await db.SaveChangesAsync();
        var repository = new EfChatRepository(db);
        var message = await repository.SendDirectMessageAsync(alice.Id, bob.Id, Guid.NewGuid(), "to bob", Array.Empty<Guid>(), default);
        await repository.SendDirectMessageAsync(alice.Id, carol.Id, Guid.NewGuid(), "to carol", Array.Empty<Guid>(), default);
        var image = DirectAttachment.Create(alice.Id, "photo.png", "chat/photo.png", 123, "image/png", DateTimeOffset.UtcNow);
        image.AttachToMessage(message.Id);
        var file = DirectAttachment.Create(alice.Id, "doc.pdf", "chat/doc.pdf", 456, "application/pdf", DateTimeOffset.UtcNow);
        file.AttachToMessage(message.Id);
        db.DirectAttachments.AddRange(image, file);
        await db.SaveChangesAsync();

        var attachments = await repository.ListAttachmentsAsync(alice.Id, bob.Id, "photos", default);

        attachments.Should().ContainSingle();
        attachments[0].Id.Should().Be(image.Id);
        attachments[0].DownloadUrl.Should().Be($"/api/chat/attachments/{image.Id}");
    }

    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }
}
