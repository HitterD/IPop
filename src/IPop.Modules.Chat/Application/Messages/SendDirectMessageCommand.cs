using MediatR;

namespace IPop.Modules.Chat.Application.Messages;

public sealed record SendDirectMessageCommand(
    Guid SenderUserId,
    Guid RecipientUserId,
    Guid ClientMessageId,
    string Body,
    IReadOnlyCollection<Guid> AttachmentIds) : IRequest<ChatMessageDto>;
