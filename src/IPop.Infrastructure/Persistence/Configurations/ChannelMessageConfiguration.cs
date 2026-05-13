using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using IPop.Modules.Chat.Domain;

namespace IPop.Infrastructure.Persistence.Configurations;

public sealed class ChannelMessageConfiguration : IEntityTypeConfiguration<ChannelMessage>
{
    public void Configure(EntityTypeBuilder<ChannelMessage> builder)
    {
        builder.ToTable("ChannelMessages");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ChannelId).IsRequired();
        builder.Property(x => x.SenderUserId).IsRequired();
        builder.Property(x => x.ClientMessageId).IsRequired();
        builder.Property(x => x.Body).IsRequired().HasMaxLength(8000);
        builder.Property(x => x.BodyPlain).IsRequired().HasMaxLength(400);
        builder.Property(x => x.MessageType).HasConversion<int>().IsRequired();
        builder.Property(x => x.SentAt).IsRequired();
        builder.HasIndex(x => new { x.ChannelId, x.SentAt });
        builder.HasIndex(x => new { x.SenderUserId, x.ClientMessageId }).IsUnique();
    }
}
