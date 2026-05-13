using MediatR;
using IPop.Modules.Chat.Application.Abstractions;

namespace IPop.Modules.Chat.Application.Messages;

public sealed class SendDirectMessageCommandHandler(IChatRepository repository) : IRequestHandler<SendDirectMessageCommand, ChatMessageDto>
{
    public Task<ChatMessageDto> Handle(SendDirectMessageCommand request, CancellationToken cancellationToken)
    {
        return repository.SendDirectMessageAsync(
            request.SenderUserId,
            request.RecipientUserId,
            request.ClientMessageId,
            request.Body,
            request.AttachmentIds,
            cancellationToken);
    }
}
