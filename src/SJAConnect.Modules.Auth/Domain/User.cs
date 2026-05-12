namespace SJAConnect.Modules.Auth.Domain;

public sealed class User
{
    private readonly List<UserRole> _roles = new();

    private User() { }

    public Guid Id { get; private set; }
    public string Nik { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string? Department { get; private set; }
    public string? Position { get; private set; }
    public string? Location { get; private set; }
    public string PasswordHash { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public string? DisabledReason { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? LastLoginAt { get; private set; }
    public IReadOnlyCollection<UserRole> Roles => _roles;

    public static User Create(string nik, string displayName, string? department, string? position, string? location, string passwordHash)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Nik = nik.Trim(),
            DisplayName = displayName.Trim(),
            Department = string.IsNullOrWhiteSpace(department) ? null : department.Trim(),
            Position = string.IsNullOrWhiteSpace(position) ? null : position.Trim(),
            Location = string.IsNullOrWhiteSpace(location) ? null : location.Trim(),
            PasswordHash = passwordHash,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void SetPasswordHash(string passwordHash) => PasswordHash = passwordHash;
    public void RecordLogin(DateTimeOffset at) => LastLoginAt = at;
    public void Disable(string reason) { IsActive = false; DisabledReason = reason; }
    public void Enable() { IsActive = true; DisabledReason = null; }
}
