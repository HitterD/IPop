using FluentAssertions;
using IPop.Modules.Chat.Domain;
using Xunit;

namespace IPop.UnitTests.Modules.Chat;

public sealed class ChannelTests
{
    [Fact]
    public void Create_AddsCreatorAsAdmin()
    {
        var creator = Guid.NewGuid();
        var channel = Channel.Create("General", null, ChannelType.Public, creator, DateTimeOffset.UnixEpoch);

        channel.Name.Should().Be("General");
        channel.NameKey.Should().Be("general");
        channel.Type.Should().Be(ChannelType.Public);
        channel.Members.Should().ContainSingle();
        var member = channel.Members.Single();
        member.UserId.Should().Be(creator);
        member.Role.Should().Be(ChannelMemberRole.Admin);
        channel.CreatedAt.Should().Be(DateTimeOffset.UnixEpoch);
        channel.LastMessageAt.Should().Be(DateTimeOffset.UnixEpoch);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_BlankName_Throws(string name)
    {
        var act = () => Channel.Create(name, null, ChannelType.Public, Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_NameTooLong_Throws()
    {
        var longName = new string('x', Channel.NameMaxLength + 1);
        var act = () => Channel.Create(longName, null, ChannelType.Public, Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_EmptyCreator_Throws()
    {
        var act = () => Channel.Create("General", null, ChannelType.Public, Guid.Empty, DateTimeOffset.UtcNow);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddMember_AppendsMember()
    {
        var creator = Guid.NewGuid();
        var channel = Channel.Create("General", null, ChannelType.Public, creator, DateTimeOffset.UtcNow);
        var member = Guid.NewGuid();

        var added = channel.AddMember(member, ChannelMemberRole.Member, DateTimeOffset.UtcNow);

        channel.Members.Should().HaveCount(2);
        added.UserId.Should().Be(member);
        added.Role.Should().Be(ChannelMemberRole.Member);
        channel.IsMember(member).Should().BeTrue();
    }

    [Fact]
    public void AddMember_Duplicate_Throws()
    {
        var creator = Guid.NewGuid();
        var channel = Channel.Create("General", null, ChannelType.Public, creator, DateTimeOffset.UtcNow);

        var act = () => channel.AddMember(creator, ChannelMemberRole.Member, DateTimeOffset.UtcNow);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void RemoveMember_NonAdmin_Succeeds()
    {
        var creator = Guid.NewGuid();
        var member = Guid.NewGuid();
        var channel = Channel.Create("General", null, ChannelType.Public, creator, DateTimeOffset.UtcNow);
        channel.AddMember(member, ChannelMemberRole.Member, DateTimeOffset.UtcNow);

        channel.RemoveMember(member);

        channel.IsMember(member).Should().BeFalse();
    }

    [Fact]
    public void RemoveMember_LastAdmin_Throws()
    {
        var creator = Guid.NewGuid();
        var channel = Channel.Create("General", null, ChannelType.Public, creator, DateTimeOffset.UtcNow);

        var act = () => channel.RemoveMember(creator);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Touch_UpdatesLastMessageAt()
    {
        var channel = Channel.Create("General", null, ChannelType.Public, Guid.NewGuid(), DateTimeOffset.UnixEpoch);
        var later = DateTimeOffset.UnixEpoch.AddMinutes(5);

        channel.Touch(later);

        channel.LastMessageAt.Should().Be(later);
    }

    [Fact]
    public void ChannelMessage_Create_TrimsBody()
    {
        var message = ChannelMessage.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "  hi  ", "  hi  ", MessageType.Text, DateTimeOffset.UnixEpoch);
        message.Body.Should().Be("hi");
        message.BodyPlain.Should().Be("hi");
    }

    [Fact]
    public void ChannelMessage_Create_BlankBody_Throws()
    {
        var act = () => ChannelMessage.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), " ", " ", MessageType.Text, DateTimeOffset.UnixEpoch);
        act.Should().Throw<ArgumentException>();
    }
}
