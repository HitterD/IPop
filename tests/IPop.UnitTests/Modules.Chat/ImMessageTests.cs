using FluentAssertions;
using IPop.Modules.Chat.Domain;
using Xunit;

namespace IPop.UnitTests.Modules.Chat;

public sealed class ImMessageTests
{
    [Fact]
    public void Create_ValidArgs_CreatesSentAndInboxRecipients()
    {
        var sender = Guid.NewGuid();
        var r1 = Guid.NewGuid();
        var r2 = Guid.NewGuid();

        var msg = ImMessage.Create("Subject", "Body", "Body", sender, [r1, r2], null, ImMessageType.New, DateTimeOffset.UtcNow);

        msg.Subject.Should().Be("Subject");
        msg.Body.Should().Be("Body");
        msg.MessageType.Should().Be(ImMessageType.New);
        msg.Recipients.Should().HaveCount(3); // sender(Sent) + r1(Inbox) + r2(Inbox)
        msg.Recipients.Count(r => r.Folder == ImFolder.Sent).Should().Be(1);
        msg.Recipients.Count(r => r.Folder == ImFolder.Inbox).Should().Be(2);
        msg.Recipients.Single(r => r.Folder == ImFolder.Sent).UserId.Should().Be(sender);
        msg.Recipients.Single(r => r.Folder == ImFolder.Sent).IsRead.Should().BeTrue();
        msg.Recipients.Where(r => r.Folder == ImFolder.Inbox).Should().AllSatisfy(r => r.IsRead.Should().BeFalse());
    }

    [Fact]
    public void Create_SenderInRecipientList_NotDuplicated()
    {
        var sender = Guid.NewGuid();
        var r1 = Guid.NewGuid();

        var msg = ImMessage.Create("Subject", "Body", "Body", sender, [sender, r1], null, ImMessageType.New, DateTimeOffset.UtcNow);

        msg.Recipients.Should().HaveCount(2); // sender(Sent) + r1(Inbox) only
    }

    [Fact]
    public void Create_BlankSubject_Throws()
    {
        var act = () => ImMessage.Create("  ", "Body", "Body", Guid.NewGuid(), [Guid.NewGuid()], null, ImMessageType.New, DateTimeOffset.UtcNow);
        act.Should().Throw<ArgumentException>().WithMessage("*Subject*");
    }

    [Fact]
    public void Create_SubjectTooLong_Throws()
    {
        var act = () => ImMessage.Create(new string('x', ImMessage.SubjectMaxLength + 1), "Body", "Body", Guid.NewGuid(), [Guid.NewGuid()], null, ImMessageType.New, DateTimeOffset.UtcNow);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_BlankBody_Throws()
    {
        var act = () => ImMessage.Create("Subject", "  ", "  ", Guid.NewGuid(), [Guid.NewGuid()], null, ImMessageType.New, DateTimeOffset.UtcNow);
        act.Should().Throw<ArgumentException>().WithMessage("*Body*");
    }

    [Fact]
    public void Create_EmptyRecipients_Throws()
    {
        var act = () => ImMessage.Create("Subject", "Body", "Body", Guid.NewGuid(), [], null, ImMessageType.New, DateTimeOffset.UtcNow);
        act.Should().Throw<ArgumentException>().WithMessage("*recipient*");
    }

    [Fact]
    public void Create_EmptyGuidInRecipients_Throws()
    {
        var act = () => ImMessage.Create("Subject", "Body", "Body", Guid.NewGuid(), [Guid.Empty], null, ImMessageType.New, DateTimeOffset.UtcNow);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_BodyPlainTruncatedTo400()
    {
        var longPlain = new string('x', 500);
        var msg = ImMessage.Create("Subject", "Body", longPlain, Guid.NewGuid(), [Guid.NewGuid()], null, ImMessageType.New, DateTimeOffset.UtcNow);
        msg.BodyPlain.Length.Should().Be(ImMessage.BodyPlainMaxLength);
    }

    [Fact]
    public void Create_WithParentMessage_SetsParentId()
    {
        var parentId = Guid.NewGuid();
        var msg = ImMessage.Create("Re: Subject", "Body", "Body", Guid.NewGuid(), [Guid.NewGuid()], parentId, ImMessageType.Reply, DateTimeOffset.UtcNow);
        msg.ParentMessageId.Should().Be(parentId);
        msg.MessageType.Should().Be(ImMessageType.Reply);
    }

    [Fact]
    public void ImRecipient_MarkRead_SetsReadAtOnce()
    {
        var recipient = ImRecipient.CreateInbox(Guid.NewGuid(), Guid.NewGuid());
        var first = DateTimeOffset.UnixEpoch.AddMinutes(1);
        var second = DateTimeOffset.UnixEpoch.AddMinutes(2);

        recipient.MarkRead(first);
        recipient.MarkRead(second);

        recipient.IsRead.Should().BeTrue();
        recipient.ReadAt.Should().Be(first);
    }

    [Fact]
    public void ImRecipient_MoveToDeleted_ChangesFolder()
    {
        var recipient = ImRecipient.CreateInbox(Guid.NewGuid(), Guid.NewGuid());
        recipient.MoveToDeleted();
        recipient.Folder.Should().Be(ImFolder.Deleted);
    }

    [Fact]
    public void ImRecipient_Restore_FromDeleted()
    {
        var recipient = ImRecipient.CreateInbox(Guid.NewGuid(), Guid.NewGuid());
        recipient.MoveToDeleted();
        recipient.Restore(ImFolder.Inbox);
        recipient.Folder.Should().Be(ImFolder.Inbox);
    }

    [Fact]
    public void ImRecipient_Restore_NotFromDeleted_NoOp()
    {
        var recipient = ImRecipient.CreateInbox(Guid.NewGuid(), Guid.NewGuid());
        recipient.Restore(ImFolder.Sent);
        recipient.Folder.Should().Be(ImFolder.Inbox);
    }

    [Fact]
    public void ImAttachment_Create_InvalidFileName_Throws()
    {
        var act = () => ImAttachment.Create(Guid.NewGuid(), Guid.NewGuid(), "../evil.exe", "path", 100, "application/octet-stream", DateTimeOffset.UtcNow);
        act.Should().Throw<ArgumentException>();
    }
}
