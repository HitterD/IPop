using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using IPop.Modules.Chat.Domain;

namespace IPop.Infrastructure.Persistence.Configurations;

public sealed class DirectAttachmentConfiguration : IEntityTypeConfiguration<DirectAttachment>
{
    public void Configure(EntityTypeBuilder<DirectAttachment> builder)
    {
        builder.ToTable("DirectAttachments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.MessageId).IsRequired(false);
        builder.Property(x => x.SenderUserId).IsRequired();
        builder.Property(x => x.OriginalName).IsRequired().HasMaxLength(260);
        builder.Property(x => x.StoragePath).IsRequired().HasMaxLength(260);
        builder.Property(x => x.SizeBytes).IsRequired();
        builder.Property(x => x.ContentType).IsRequired().HasMaxLength(127);
        builder.Property(x => x.UploadedAt).IsRequired();
        builder.Property(x => x.ScanStatus).IsRequired();
        builder.HasIndex(x => x.MessageId);
        builder.HasIndex(x => new { x.SenderUserId, x.UploadedAt });
        builder.HasOne(x => x.Message)
            .WithMany(x => x.Attachments)
            .HasForeignKey(x => x.MessageId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
