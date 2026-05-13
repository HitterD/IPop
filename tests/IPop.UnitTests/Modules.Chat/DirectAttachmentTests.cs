using FluentAssertions;
using IPop.Modules.Chat.Domain;
using Xunit;

namespace IPop.UnitTests.Modules.Chat;

public sealed class DirectAttachmentTests
{
    [Fact]
    public void Create_ValidAttachment_SetsFields()
    {
        var senderId = Guid.NewGuid();
        var attachment = DirectAttachment.Create(senderId, "report.pdf", "chat/2026/05/file.pdf", 1024, "application/pdf", DateTimeOffset.UnixEpoch);
        attachment.SenderUserId.Should().Be(senderId);
        attachment.OriginalName.Should().Be("report.pdf");
        attachment.ScanStatus.Should().Be(AttachmentScanStatus.Pending);
    }

    [Fact]
    public void Create_PathTraversalName_Throws()
    {
        var act = () => DirectAttachment.Create(Guid.NewGuid(), "../evil.exe", "chat/x", 1, "application/pdf", DateTimeOffset.UnixEpoch);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AttachToMessage_SetsMessageIdOnce()
    {
        var messageId = Guid.NewGuid();
        var attachment = DirectAttachment.Create(Guid.NewGuid(), "a.png", "chat/a.png", 10, "image/png", DateTimeOffset.UnixEpoch);
        attachment.AttachToMessage(messageId);
        attachment.MessageId.Should().Be(messageId);
    }
}
