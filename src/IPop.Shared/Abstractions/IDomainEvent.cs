using MediatR;

namespace SJAConnect.Shared.Abstractions;

public interface IDomainEvent : INotification
{
    Guid EventId { get; }
    DateTimeOffset OccurredAt { get; }
}
