using MediatR;

namespace IPop.Shared.Abstractions;

public interface IDomainEvent : INotification
{
    Guid EventId { get; }
    DateTimeOffset OccurredAt { get; }
}
