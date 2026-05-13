namespace IPop.Shared.Domain;

public readonly record struct IdempotencyKey(Guid Value)
{
    public static IdempotencyKey New() => new(Guid.NewGuid());
    public static IdempotencyKey Parse(string s) => new(Guid.Parse(s));
    public override string ToString() => Value.ToString();
}
