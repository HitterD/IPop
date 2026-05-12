using SJAConnect.Shared.Abstractions;

namespace SJAConnect.Shared.Domain;

public readonly record struct Nik
{
    public string Value { get; }
    private Nik(string v) => Value = v;

    public static Result<Nik> Create(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return Result<Nik>.Fail(new Error("nik.empty", "NIK is required"));
        }

        var trimmed = raw.Trim();
        if (trimmed.Length > 32)
        {
            return Result<Nik>.Fail(new Error("nik.too_long", "NIK max 32 characters"));
        }

        return Result<Nik>.Ok(new Nik(trimmed));
    }

    public override string ToString() => Value;
}
