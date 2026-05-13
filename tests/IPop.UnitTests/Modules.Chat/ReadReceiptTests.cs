using FluentAssertions;
using IPop.Infrastructure.Chat;
using IPop.Infrastructure.Persistence;
using IPop.Modules.Auth.Domain;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IPop.UnitTests.Modules.Chat;

public sealed class ReadReceiptTests
{
    [Fact]
    public async Task MarkDeliveredAsync_SetsDeliveredAtOnce()
    {
        await using var db = CreateDbContext();
        var (sender, recipient) = await SeedUsersAsync(db);
        var repository = new EfChatRepository(db);
        var message = await repository.SendDirectMessageAsync(sender.Id, recipient.Id, Guid.NewGuid(), "hello", Array.Empty<Guid>(), CancellationToken.None);

        var firstDeliveredAt = DateTimeOffset.UnixEpoch.AddMinutes(1);
        var secondDeliveredAt = DateTimeOffset.UnixEpoch.AddMinutes(2);
        await repository.MarkDeliveredAsync(message.Id, firstDeliveredAt, CancellationToken.None);
        await repository.MarkDeliveredAsync(message.Id, secondDeliveredAt, CancellationToken.None);

        var stored = await db.DirectMessages.SingleAsync(x => x.Id == message.Id);
        stored.DeliveredAt.Should().Be(firstDeliveredAt);
    }

    [Fact]
    public async Task MarkReadAsync_OnlyMarksMessagesWhereCurrentUserIsRecipient()
    {
        await using var db = CreateDbContext();
        var (currentUser, otherUser) = await SeedUsersAsync(db);
        var repository = new EfChatRepository(db);
        var inbound = await repository.SendDirectMessageAsync(otherUser.Id, currentUser.Id, Guid.NewGuid(), "inbound", Array.Empty<Guid>(), CancellationToken.None);
        var outbound = await repository.SendDirectMessageAsync(currentUser.Id, otherUser.Id, Guid.NewGuid(), "outbound", Array.Empty<Guid>(), CancellationToken.None);

        var readAt = DateTimeOffset.UnixEpoch.AddMinutes(3);
        var affectedSenders = await repository.MarkReadAsync(currentUser.Id, [inbound.Id, outbound.Id], readAt, CancellationToken.None);

        affectedSenders.Should().BeEquivalentTo([otherUser.Id]);
        var inboundStored = await db.DirectMessages.SingleAsync(x => x.Id == inbound.Id);
        var outboundStored = await db.DirectMessages.SingleAsync(x => x.Id == outbound.Id);
        inboundStored.ReadAt.Should().Be(readAt);
        outboundStored.ReadAt.Should().BeNull();
    }

    [Fact]
    public async Task MarkUndeliveredMessagesDeliveredAsync_MarksRecipientUndeliveredMessagesAndReturnsDtos()
    {
        await using var db = CreateDbContext();
        var (sender, recipient) = await SeedUsersAsync(db);
        var thirdUser = User.Create("300", "Third User", null, null, null, "hash");
        await db.Users.AddAsync(thirdUser);
        await db.SaveChangesAsync();

        var repository = new EfChatRepository(db);
        var deliveredMessage = await repository.SendDirectMessageAsync(sender.Id, recipient.Id, Guid.NewGuid(), "already delivered", Array.Empty<Guid>(), CancellationToken.None);
        await repository.MarkDeliveredAsync(deliveredMessage.Id, DateTimeOffset.UnixEpoch.AddMinutes(1), CancellationToken.None);
        var undeliveredMessage = await repository.SendDirectMessageAsync(sender.Id, recipient.Id, Guid.NewGuid(), "needs delivery", Array.Empty<Guid>(), CancellationToken.None);
        var otherRecipientMessage = await repository.SendDirectMessageAsync(sender.Id, thirdUser.Id, Guid.NewGuid(), "other recipient", Array.Empty<Guid>(), CancellationToken.None);

        var deliveredAt = DateTimeOffset.UnixEpoch.AddMinutes(4);
        var delivered = await repository.MarkUndeliveredMessagesDeliveredAsync(recipient.Id, deliveredAt, CancellationToken.None);

        delivered.Select(x => x.Id).Should().BeEquivalentTo([undeliveredMessage.Id]);
        delivered.Single().DeliveredAt.Should().Be(deliveredAt);
        (await db.DirectMessages.SingleAsync(x => x.Id == undeliveredMessage.Id)).DeliveredAt.Should().Be(deliveredAt);
        (await db.DirectMessages.SingleAsync(x => x.Id == deliveredMessage.Id)).DeliveredAt.Should().Be(DateTimeOffset.UnixEpoch.AddMinutes(1));
        (await db.DirectMessages.SingleAsync(x => x.Id == otherRecipientMessage.Id)).DeliveredAt.Should().BeNull();
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static async Task<(User Sender, User Recipient)> SeedUsersAsync(AppDbContext db)
    {
        var sender = User.Create("100", "Sender User", null, null, null, "hash");
        var recipient = User.Create("200", "Recipient User", null, null, null, "hash");
        await db.Users.AddRangeAsync(sender, recipient);
        await db.SaveChangesAsync();
        return (sender, recipient);
    }
}
