using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using IPop.Modules.Chat.Domain;

namespace IPop.Infrastructure.Persistence.Configurations;

public sealed class ChannelConfiguration : IEntityTypeConfiguration<Channel>
{
    public void Configure(EntityTypeBuilder<Channel> builder)
    {
        builder.ToTable("Channels");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(Channel.NameMaxLength);
        builder.Property(x => x.NameKey).IsRequired().HasMaxLength(Channel.NameMaxLength);
        builder.Property(x => x.Description).HasMaxLength(Channel.DescriptionMaxLength);
        builder.Property(x => x.Type).HasConversion<int>().IsRequired();
        builder.Property(x => x.CreatedByUserId).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.LastMessageAt).IsRequired();
        builder.HasIndex(x => x.NameKey).IsUnique();
        builder.HasIndex(x => x.LastMessageAt);

        var members = builder.Metadata.FindNavigation(nameof(Channel.Members))!;
        members.SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.HasMany(x => x.Members)
            .WithOne()
            .HasForeignKey(x => x.ChannelId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
