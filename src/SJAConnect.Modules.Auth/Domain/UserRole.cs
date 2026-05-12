namespace SJAConnect.Modules.Auth.Domain;

public sealed class UserRole
{
    private UserRole() { }
    public Guid UserId { get; private set; }
    public Guid RoleId { get; private set; }
    public User? User { get; private set; }
    public Role? Role { get; private set; }

    public static UserRole Create(Guid userId, Guid roleId) => new() { UserId = userId, RoleId = roleId };
}
