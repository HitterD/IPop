using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using IPop.Modules.Auth.Domain;

namespace IPop.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Nik).HasMaxLength(32).IsRequired();
        builder.HasIndex(x => x.Nik).IsUnique();
        builder.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Department).HasMaxLength(200);
        builder.Property(x => x.Position).HasMaxLength(200);
        builder.Property(x => x.Location).HasMaxLength(200);
        builder.Property(x => x.PasswordHash).HasMaxLength(512).IsRequired();
        builder.Property(x => x.DisabledReason).HasMaxLength(500);
        builder.HasMany(x => x.Roles).WithOne(x => x.User).HasForeignKey(x => x.UserId);
    }
}
