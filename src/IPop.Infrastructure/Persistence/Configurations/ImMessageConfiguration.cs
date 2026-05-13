using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using IPop.Modules.Chat.Domain;

namespace IPop.Infrastructure.Persistence.Configurations;

public sealed class ImMessageConfiguration : IEntityTypeConfiguration<ImMessage>
{
    public void Configure(EntityTypeBuilder<ImMessage> builder)
    {
        builder.ToTable("ImMessages");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Subject).IsRequired().HasMaxLength(ImMessage.SubjectMaxLength);
        builder.Property(x => x.Body).IsRequired().HasMaxLength(ImMessage.BodyMaxLength);
        builder.Property(x => x.BodyPlain).IsRequired().HasMaxLength(ImMessage.BodyPlainMaxLength);
        builder.Property(x => x.SenderUserId).IsRequired();
        builder.Property(x => x.ParentMessageId).IsRequired(false);
        builder.Property(x => x.MessageType).HasConversion<int>().IsRequired();
        builder.Property(x => x.SentAt).IsRequired();
        builder.HasIndex(x => new { x.SenderUserId, x.SentAt });
        builder.HasIndex(x => x.ParentMessageId);

        var recipients = builder.Metadata.FindNavigation(nameof(ImMessage.Recipients))!;
        recipients.SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.HasMany(x => x.Recipients)
            .WithOne()
            .HasForeignKey(x => x.MessageId)
            .OnDelete(DeleteBehavior.Cascade);

        var attachments = builder.Metadata.FindNavigation(nameof(ImMessage.Attachments))!;
        attachments.SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.HasMany(x => x.Attachments)
            .WithOne()
            .HasForeignKey(x => x.MessageId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
