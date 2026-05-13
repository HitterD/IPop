using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using IPop.Modules.Chat.Domain;

namespace IPop.Infrastructure.Persistence.Configurations;

public sealed class DirectMessageConfiguration : IEntityTypeConfiguration<DirectMessage>
{
    public void Configure(EntityTypeBuilder<DirectMessage> builder)
    {
        builder.ToTable("DirectMessages");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ConversationId).IsRequired();
        builder.Property(x => x.SenderUserId).IsRequired();
        builder.Property(x => x.RecipientUserId).IsRequired();
        builder.Property(x => x.ClientMessageId).IsRequired();
        builder.Property(x => x.Body).IsRequired().HasMaxLength(8000);
        builder.Property(x => x.BodyPlain).IsRequired().HasMaxLength(400);
        builder.Property(x => x.MessageType).IsRequired();
        builder.Property(x => x.SentAt).IsRequired();
        builder.Property(x => x.DeliveredAt).IsRequired(false);
        builder.Property(x => x.ReadAt).IsRequired(false);
        builder.HasIndex(x => new { x.SenderUserId, x.ClientMessageId }).IsUnique();
        builder.HasIndex(x => new { x.ConversationId, x.SentAt });
        builder.HasOne(x => x.Conversation)
            .WithMany()
            .HasForeignKey(x => x.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
