using MediatR;
using IPop.Modules.Chat.Application.Abstractions;
using IPop.Modules.Chat.Application.Im;
using IPop.Modules.Chat.Domain;

namespace IPop.Modules.Chat.Application.Im;

public sealed class SendImCommandHandler(IImRepository repository) : IRequestHandler<SendImCommand, ImMessageSummary>
{
    public Task<ImMessageSummary> Handle(SendImCommand request, CancellationToken cancellationToken)
    {
        return repository.SendAsync(
            request.SenderUserId,
            request.RecipientUserIds,
            request.Subject,
            request.Body,
            request.ParentMessageId,
            request.MessageType,
            cancellationToken);
    }
}

public sealed class MarkImReadCommandHandler(IImRepository repository) : IRequestHandler<MarkImReadCommand>
{
    public Task Handle(MarkImReadCommand request, CancellationToken cancellationToken)
    {
        return repository.MarkReadAsync(request.CurrentUserId, request.MessageId, DateTimeOffset.UtcNow, cancellationToken);
    }
}

public sealed class DeleteImCommandHandler(IImRepository repository) : IRequestHandler<DeleteImCommand>
{
    public Task Handle(DeleteImCommand request, CancellationToken cancellationToken)
    {
        return repository.DeleteAsync(request.CurrentUserId, request.MessageIds, cancellationToken);
    }
}
