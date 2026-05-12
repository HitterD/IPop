namespace SJAConnect.Shared.Time;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
