using Microsoft.EntityFrameworkCore;
using IPop.Modules.Auth.Domain;
using IPop.Modules.Chat.Domain;

namespace IPop.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<DirectConversation> DirectConversations => Set<DirectConversation>();
    public DbSet<DirectMessage> DirectMessages => Set<DirectMessage>();
    public DbSet<DirectAttachment> DirectAttachments => Set<DirectAttachment>();
    public DbSet<Channel> Channels => Set<Channel>();
    public DbSet<ChannelMember> ChannelMembers => Set<ChannelMember>();
    public DbSet<ChannelMessage> ChannelMessages => Set<ChannelMessage>();
    public DbSet<ImMessage> ImMessages => Set<ImMessage>();
    public DbSet<ImRecipient> ImRecipients => Set<ImRecipient>();
    public DbSet<ImAttachment> ImAttachments => Set<ImAttachment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
