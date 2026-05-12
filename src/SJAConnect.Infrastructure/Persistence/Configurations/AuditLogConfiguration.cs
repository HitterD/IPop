using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SJAConnect.Modules.Auth.Domain;

namespace SJAConnect.Infrastructure.Persistence.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLog");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EventType).HasMaxLength(64).IsRequired();
        builder.Property(x => x.ActorNik).HasMaxLength(32);
        builder.Property(x => x.TargetResource).HasMaxLength(128);
        builder.Property(x => x.IpAddress).HasMaxLength(64);
        builder.Property(x => x.UserAgent).HasMaxLength(256);
        builder.HasIndex(x => new { x.EventType, x.CreatedAt });
        builder.HasIndex(x => x.ActorUserId);
        builder.HasIndex(x => x.TargetUserId);
    }
}
