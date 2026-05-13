namespace IPop.Shared.Time;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
