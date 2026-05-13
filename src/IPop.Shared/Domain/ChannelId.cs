namespace IPop.Shared.Domain;

public readonly record struct ChannelId(Guid Value)
{
    public static ChannelId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}
