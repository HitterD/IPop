using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using IPop.Modules.Chat.Domain;

namespace IPop.Infrastructure.Persistence.Configurations;

public sealed class DirectConversationConfiguration : IEntityTypeConfiguration<DirectConversation>
{
    public void Configure(EntityTypeBuilder<DirectConversation> builder)
    {
        builder.ToTable("DirectConversations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserAId).IsRequired();
        builder.Property(x => x.UserBId).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.LastMessageAt).IsRequired();
        builder.HasIndex(x => new { x.UserAId, x.UserBId }).IsUnique();
        builder.HasIndex(x => new { x.UserAId, x.LastMessageAt });
        builder.HasIndex(x => new { x.UserBId, x.LastMessageAt });
    }
}
