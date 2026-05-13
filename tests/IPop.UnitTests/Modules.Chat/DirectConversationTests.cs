using FluentAssertions;
using IPop.Modules.Chat.Domain;
using Xunit;

namespace IPop.UnitTests.Modules.Chat;

public sealed class DirectConversationTests
{
    [Fact]
    public void Create_NormalizesUserPair()
    {
        var high = Guid.Parse("99999999-9999-9999-9999-999999999999");
        var low = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var conversation = DirectConversation.Create(high, low, DateTimeOffset.UnixEpoch);

        conversation.UserAId.Should().Be(low);
        conversation.UserBId.Should().Be(high);
        conversation.CreatedAt.Should().Be(DateTimeOffset.UnixEpoch);
        conversation.LastMessageAt.Should().Be(DateTimeOffset.UnixEpoch);
    }

    [Fact]
    public void Create_SameUser_Throws()
    {
        var userId = Guid.NewGuid();
        var act = () => DirectConversation.Create(userId, userId, DateTimeOffset.UnixEpoch);
        act.Should().Throw<ArgumentException>().WithMessage("Direct conversation requires two different users.*");
    }

    [Fact]
    public void Touch_UpdatesLastMessageAt()
    {
        var conversation = DirectConversation.Create(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UnixEpoch);
        var later = DateTimeOffset.UnixEpoch.AddMinutes(5);

        conversation.Touch(later);

        conversation.LastMessageAt.Should().Be(later);
    }

    [Fact]
    public void DirectMessage_Create_RequiresBody()
    {
        var act = () => DirectMessage.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), " ", " ", MessageType.Text, DateTimeOffset.UnixEpoch);
        act.Should().Throw<ArgumentException>().WithMessage("Message body is required.*");
    }

    [Fact]
    public void DirectMessage_Create_TrimsBody()
    {
        var message = DirectMessage.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "  hello  ", "  hello  ", MessageType.Text, DateTimeOffset.UnixEpoch);
        message.Body.Should().Be("hello");
    }
}
