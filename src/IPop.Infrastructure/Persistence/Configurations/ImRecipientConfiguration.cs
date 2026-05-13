using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using IPop.Modules.Chat.Domain;

namespace IPop.Infrastructure.Persistence.Configurations;

public sealed class ImRecipientConfiguration : IEntityTypeConfiguration<ImRecipient>
{
    public void Configure(EntityTypeBuilder<ImRecipient> builder)
    {
        builder.ToTable("ImRecipients");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.MessageId).IsRequired();
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.Folder).HasConversion<int>().IsRequired();
        builder.Property(x => x.IsRead).IsRequired();
        builder.Property(x => x.ReadAt).IsRequired(false);
        builder.HasIndex(x => new { x.UserId, x.Folder, x.IsRead });
        builder.HasIndex(x => new { x.MessageId, x.UserId }).IsUnique();
    }
}
