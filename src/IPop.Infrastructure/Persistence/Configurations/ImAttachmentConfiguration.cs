using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using IPop.Modules.Chat.Domain;

namespace IPop.Infrastructure.Persistence.Configurations;

public sealed class ImAttachmentConfiguration : IEntityTypeConfiguration<ImAttachment>
{
    public void Configure(EntityTypeBuilder<ImAttachment> builder)
    {
        builder.ToTable("ImAttachments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.MessageId).IsRequired();
        builder.Property(x => x.UploaderUserId).IsRequired();
        builder.Property(x => x.OriginalName).IsRequired().HasMaxLength(260);
        builder.Property(x => x.StoragePath).IsRequired().HasMaxLength(1000);
        builder.Property(x => x.SizeBytes).IsRequired();
        builder.Property(x => x.ContentType).IsRequired().HasMaxLength(128);
        builder.Property(x => x.UploadedAt).IsRequired();
        builder.HasIndex(x => x.MessageId);
    }
}
