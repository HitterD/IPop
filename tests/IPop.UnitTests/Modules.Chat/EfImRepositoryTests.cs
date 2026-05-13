using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using IPop.Infrastructure.Chat;
using IPop.Infrastructure.Persistence;
using IPop.Modules.Auth.Domain;
using IPop.Modules.Chat.Domain;
using Xunit;

namespace IPop.UnitTests.Modules.Chat;

public sealed class EfImRepositoryTests
{
    [Fact]
    public async Task SendAsync_CreatesSentAndInboxRows()
    {
        await using var db = CreateDb();
        var (sender, r1, r2) = await SeedUsersAsync(db);
        var repo = new EfImRepository(db);

        var summary = await repo.SendAsync(sender.Id, [r1.Id, r2.Id], "Hello", "Body text", null, ImMessageType.New, default);

        summary.Subject.Should().Be("Hello");
        summary.Folder.Should().Be(ImFolder.Sent);
        summary.IsRead.Should().BeTrue();
        (await db.ImMessages.CountAsync()).Should().Be(1);
        (await db.ImRecipients.CountAsync()).Should().Be(3); // sender(Sent) + r1(Inbox) + r2(Inbox)
    }

    [Fact]
    public async Task SendAsync_DisabledRecipient_Throws()
    {
        await using var db = CreateDb();
        var (sender, r1, _) = await SeedUsersAsync(db);
        r1.Disable("admin");
        await db.SaveChangesAsync();
        var repo = new EfImRepository(db);

        var act = async () => await repo.SendAsync(sender.Id, [r1.Id], "Hello", "Body", null, ImMessageType.New, default);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ListAsync_Inbox_ReturnsOnlyInboxRows()
    {
        await using var db = CreateDb();
        var (sender, r1, _) = await SeedUsersAsync(db);
        var repo = new EfImRepository(db);
        await repo.SendAsync(sender.Id, [r1.Id], "Msg1", "Body1", null, ImMessageType.New, default);
        await repo.SendAsync(sender.Id, [r1.Id], "Msg2", "Body2", null, ImMessageType.New, default);

        var inbox = await repo.ListAsync(r1.Id, ImFolder.Inbox, default);

        inbox.Should().HaveCount(2);
        inbox.Should().AllSatisfy(x => x.Folder.Should().Be(ImFolder.Inbox));
        inbox.Should().AllSatisfy(x => x.IsRead.Should().BeFalse());
    }

    [Fact]
    public async Task ListAsync_Sent_ReturnsSentRows()
    {
        await using var db = CreateDb();
        var (sender, r1, _) = await SeedUsersAsync(db);
        var repo = new EfImRepository(db);
        await repo.SendAsync(sender.Id, [r1.Id], "Msg1", "Body1", null, ImMessageType.New, default);

        var sent = await repo.ListAsync(sender.Id, ImFolder.Sent, default);

        sent.Should().ContainSingle();
        sent[0].Folder.Should().Be(ImFolder.Sent);
        sent[0].IsRead.Should().BeTrue();
    }

    [Fact]
    public async Task GetDetailAsync_NonRecipient_ReturnsNull()
    {
        await using var db = CreateDb();
        var (sender, r1, r2) = await SeedUsersAsync(db);
        var repo = new EfImRepository(db);
        var summary = await repo.SendAsync(sender.Id, [r1.Id], "Hello", "Body", null, ImMessageType.New, default);

        var detail = await repo.GetDetailAsync(r2.Id, summary.Id, default);

        detail.Should().BeNull();
    }

    [Fact]
    public async Task GetDetailAsync_Recipient_ReturnsDetail()
    {
        await using var db = CreateDb();
        var (sender, r1, _) = await SeedUsersAsync(db);
        var repo = new EfImRepository(db);
        var summary = await repo.SendAsync(sender.Id, [r1.Id], "Hello", "Body text", null, ImMessageType.New, default);

        var detail = await repo.GetDetailAsync(r1.Id, summary.Id, default);

        detail.Should().NotBeNull();
        detail!.Subject.Should().Be("Hello");
        detail.Body.Should().Be("Body text");
        detail.SenderDisplayName.Should().Be("Alice");
        detail.Recipients.Should().HaveCount(2); // sender + r1
    }

    [Fact]
    public async Task MarkReadAsync_UpdatesIsRead()
    {
        await using var db = CreateDb();
        var (sender, r1, _) = await SeedUsersAsync(db);
        var repo = new EfImRepository(db);
        var summary = await repo.SendAsync(sender.Id, [r1.Id], "Hello", "Body", null, ImMessageType.New, default);

        await repo.MarkReadAsync(r1.Id, summary.Id, DateTimeOffset.UtcNow, default);

        var recipient = await db.ImRecipients.SingleAsync(r => r.MessageId == summary.Id && r.UserId == r1.Id);
        recipient.IsRead.Should().BeTrue();
        recipient.ReadAt.Should().NotBeNull();
    }

    [Fact]
    public async Task MarkReadAsync_NonRecipient_NoOp()
    {
        await using var db = CreateDb();
        var (sender, r1, r2) = await SeedUsersAsync(db);
        var repo = new EfImRepository(db);
        var summary = await repo.SendAsync(sender.Id, [r1.Id], "Hello", "Body", null, ImMessageType.New, default);

        var act = async () => await repo.MarkReadAsync(r2.Id, summary.Id, DateTimeOffset.UtcNow, default);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task DeleteAsync_MovesToDeletedFolder()
    {
        await using var db = CreateDb();
        var (sender, r1, _) = await SeedUsersAsync(db);
        var repo = new EfImRepository(db);
        var summary = await repo.SendAsync(sender.Id, [r1.Id], "Hello", "Body", null, ImMessageType.New, default);

        await repo.DeleteAsync(r1.Id, [summary.Id], default);

        var recipient = await db.ImRecipients.SingleAsync(r => r.MessageId == summary.Id && r.UserId == r1.Id);
        recipient.Folder.Should().Be(ImFolder.Deleted);
        // Sender Sent row should be unaffected
        var senderRow = await db.ImRecipients.SingleAsync(r => r.MessageId == summary.Id && r.UserId == sender.Id);
        senderRow.Folder.Should().Be(ImFolder.Sent);
    }

    [Fact]
    public async Task GetUnreadCountAsync_CountsInboxUnread()
    {
        await using var db = CreateDb();
        var (sender, r1, _) = await SeedUsersAsync(db);
        var repo = new EfImRepository(db);
        var s1 = await repo.SendAsync(sender.Id, [r1.Id], "Msg1", "Body", null, ImMessageType.New, default);
        await repo.SendAsync(sender.Id, [r1.Id], "Msg2", "Body", null, ImMessageType.New, default);
        await repo.MarkReadAsync(r1.Id, s1.Id, DateTimeOffset.UtcNow, default);

        var count = await repo.GetUnreadCountAsync(r1.Id, default);

        count.Should().Be(1);
    }

    [Fact]
    public async Task GetDetailAsync_IncludesThreadReplies()
    {
        await using var db = CreateDb();
        var (sender, r1, _) = await SeedUsersAsync(db);
        var repo = new EfImRepository(db);
        var parent = await repo.SendAsync(sender.Id, [r1.Id], "Original", "Body", null, ImMessageType.New, default);
        await repo.SendAsync(r1.Id, [sender.Id], "Re: Original", "Reply body", parent.Id, ImMessageType.Reply, default);

        var detail = await repo.GetDetailAsync(r1.Id, parent.Id, default);

        detail.Should().NotBeNull();
        detail!.ThreadReplies.Should().ContainSingle();
        detail.ThreadReplies[0].Subject.Should().Be("Re: Original");
    }

    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static async Task<(User Sender, User R1, User R2)> SeedUsersAsync(AppDbContext db)
    {
        var sender = User.Create("1001", "Alice", "IT", "Dev", "HQ", "hash");
        var r1 = User.Create("1002", "Bob", "IT", "Dev", "HQ", "hash");
        var r2 = User.Create("1003", "Carol", "IT", "Dev", "HQ", "hash");
        db.Users.AddRange(sender, r1, r2);
        await db.SaveChangesAsync();
        return (sender, r1, r2);
    }
}
