using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using IPop.Infrastructure.Persistence;
using IPop.Modules.Chat.Domain;
using Xunit;

namespace IPop.UnitTests.Modules.Chat;

public sealed class ChatModelConfigurationTests
{
    [Fact]
    public void AppDbContext_Model_ContainsChatEntitiesAndIndexes()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase("chat-model-test").Options;
        using var db = new AppDbContext(options);

        var conversation = db.Model.FindEntityType(typeof(DirectConversation));
        var message = db.Model.FindEntityType(typeof(DirectMessage));
        var attachment = db.Model.FindEntityType(typeof(DirectAttachment));

        conversation.Should().NotBeNull();
        message.Should().NotBeNull();
        attachment.Should().NotBeNull();
        conversation!.GetIndexes().Should().Contain(i => i.IsUnique && i.Properties.Select(p => p.Name).SequenceEqual(new[] { "UserAId", "UserBId" }));
        message!.GetIndexes().Should().Contain(i => i.IsUnique && i.Properties.Select(p => p.Name).SequenceEqual(new[] { "SenderUserId", "ClientMessageId" }));
        message.FindProperty(nameof(DirectMessage.BodyPlain))!.GetMaxLength().Should().Be(400);
        attachment!.GetIndexes().Should().Contain(i => i.Properties.Select(p => p.Name).SequenceEqual(new[] { "MessageId" }));
        attachment.GetIndexes().Should().Contain(i => i.Properties.Select(p => p.Name).SequenceEqual(new[] { "SenderUserId", "UploadedAt" }));
    }
}
