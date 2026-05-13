using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using IPop.Infrastructure.Chat;
using IPop.Infrastructure.Persistence;
using IPop.Modules.Auth.Domain;
using IPop.Modules.Chat.Application.Channels;
using Xunit;

namespace IPop.UnitTests.Modules.Chat;

public sealed class EfChannelRepositoryTests
{
    [Fact]
    public async Task CreateAsync_PersistsChannelWithCreatorAsAdmin()
    {
        await using var db = CreateDb();
        var creator = User.Create("1001", "Alice", "IT", "Dev", "HQ", "hash");
        db.Users.Add(creator);
        await db.SaveChangesAsync();
        var repo = new EfChannelRepository(db);

        var summary = await repo.CreateAsync(creator.Id, "General", "team chat", ChannelTypeDto.Public, default);

        summary.Name.Should().Be("General");
        summary.MemberCount.Should().Be(1);
        summary.IsMember.Should().BeTrue();
        (await db.Channels.CountAsync()).Should().Be(1);
        (await db.ChannelMembers.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task CreateAsync_DuplicateName_Throws()
    {
        await using var db = CreateDb();
        var creator = User.Create("1001", "Alice", "IT", "Dev", "HQ", "hash");
        db.Users.Add(creator);
        await db.SaveChangesAsync();
        var repo = new EfChannelRepository(db);
        await repo.CreateAsync(creator.Id, "General", null, ChannelTypeDto.Public, default);

        var act = async () => await repo.CreateAsync(creator.Id, "general", null, ChannelTypeDto.Public, default);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task JoinAsync_PublicChannel_AddsMember()
    {
        await using var db = CreateDb();
        var creator = User.Create("1001", "Alice", "IT", "Dev", "HQ", "hash");
        var joiner = User.Create("1002", "Bob", "IT", "Dev", "HQ", "hash");
        db.Users.AddRange(creator, joiner);
        await db.SaveChangesAsync();
        var repo = new EfChannelRepository(db);
        var channel = await repo.CreateAsync(creator.Id, "General", null, ChannelTypeDto.Public, default);

        var member = await repo.JoinAsync(channel.ChannelId, joiner.Id, default);

        member.UserId.Should().Be(joiner.Id);
        member.DisplayName.Should().Be("Bob");
        (await db.ChannelMembers.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task JoinAsync_PrivateChannel_Throws()
    {
        await using var db = CreateDb();
        var creator = User.Create("1001", "Alice", "IT", "Dev", "HQ", "hash");
        var joiner = User.Create("1002", "Bob", "IT", "Dev", "HQ", "hash");
        db.Users.AddRange(creator, joiner);
        await db.SaveChangesAsync();
        var repo = new EfChannelRepository(db);
        var channel = await repo.CreateAsync(creator.Id, "Secrets", null, ChannelTypeDto.Private, default);

        var act = async () => await repo.JoinAsync(channel.ChannelId, joiner.Id, default);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task SendMessageAsync_NonMember_Throws()
    {
        await using var db = CreateDb();
        var creator = User.Create("1001", "Alice", "IT", "Dev", "HQ", "hash");
        var outsider = User.Create("1002", "Bob", "IT", "Dev", "HQ", "hash");
        db.Users.AddRange(creator, outsider);
        await db.SaveChangesAsync();
        var repo = new EfChannelRepository(db);
        var channel = await repo.CreateAsync(creator.Id, "General", null, ChannelTypeDto.Public, default);

        var act = async () => await repo.SendMessageAsync(channel.ChannelId, outsider.Id, Guid.NewGuid(), "hi", default);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task SendMessageAsync_PersistsMessage()
    {
        await using var db = CreateDb();
        var creator = User.Create("1001", "Alice", "IT", "Dev", "HQ", "hash");
        db.Users.Add(creator);
        await db.SaveChangesAsync();
        var repo = new EfChannelRepository(db);
        var channel = await repo.CreateAsync(creator.Id, "General", null, ChannelTypeDto.Public, default);

        var message = await repo.SendMessageAsync(channel.ChannelId, creator.Id, Guid.NewGuid(), "hello", default);

        message.Body.Should().Be("hello");
        message.SenderDisplayName.Should().Be("Alice");
        (await db.ChannelMessages.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task SendMessageAsync_SameClientMessageId_ReturnsExisting()
    {
        await using var db = CreateDb();
        var creator = User.Create("1001", "Alice", "IT", "Dev", "HQ", "hash");
        db.Users.Add(creator);
        await db.SaveChangesAsync();
        var repo = new EfChannelRepository(db);
        var channel = await repo.CreateAsync(creator.Id, "General", null, ChannelTypeDto.Public, default);
        var clientId = Guid.NewGuid();

        var first = await repo.SendMessageAsync(channel.ChannelId, creator.Id, clientId, "hello", default);
        var second = await repo.SendMessageAsync(channel.ChannelId, creator.Id, clientId, "hello retry", default);

        second.Id.Should().Be(first.Id);
        second.Body.Should().Be("hello");
        (await db.ChannelMessages.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task ListMessagesAsync_NonMember_Throws()
    {
        await using var db = CreateDb();
        var creator = User.Create("1001", "Alice", "IT", "Dev", "HQ", "hash");
        var outsider = User.Create("1002", "Bob", "IT", "Dev", "HQ", "hash");
        db.Users.AddRange(creator, outsider);
        await db.SaveChangesAsync();
        var repo = new EfChannelRepository(db);
        var channel = await repo.CreateAsync(creator.Id, "General", null, ChannelTypeDto.Public, default);

        var act = async () => await repo.ListMessagesAsync(channel.ChannelId, outsider.Id, default);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ListAsync_HidesPrivateChannelsFromNonMembers()
    {
        await using var db = CreateDb();
        var creator = User.Create("1001", "Alice", "IT", "Dev", "HQ", "hash");
        var outsider = User.Create("1002", "Bob", "IT", "Dev", "HQ", "hash");
        db.Users.AddRange(creator, outsider);
        await db.SaveChangesAsync();
        var repo = new EfChannelRepository(db);
        await repo.CreateAsync(creator.Id, "Secrets", null, ChannelTypeDto.Private, default);
        await repo.CreateAsync(creator.Id, "Public", null, ChannelTypeDto.Public, default);

        var visible = await repo.ListAsync(outsider.Id, default);

        visible.Should().ContainSingle();
        visible[0].Name.Should().Be("Public");
        visible[0].IsMember.Should().BeFalse();
    }

    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }
}
