namespace IPop.Modules.Auth.Domain;

public sealed class Role
{
    private Role() { }
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;

    public static Role Create(string name) => new() { Id = Guid.NewGuid(), Name = name.Trim() };
}
