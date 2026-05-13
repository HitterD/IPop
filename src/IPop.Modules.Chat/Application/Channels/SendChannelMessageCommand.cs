using MediatR;

namespace IPop.Modules.Chat.Application.Channels;

public sealed record SendChannelMessageCommand(
    Guid ChannelId,
    Guid SenderUserId,
    Guid ClientMessageId,
    string Body) : IRequest<ChannelMessageDto>;
