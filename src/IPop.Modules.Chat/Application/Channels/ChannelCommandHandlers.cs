using MediatR;
using IPop.Modules.Chat.Application.Abstractions;

namespace IPop.Modules.Chat.Application.Channels;

public sealed class CreateChannelCommandHandler(IChannelRepository channels) : IRequestHandler<CreateChannelCommand, ChannelSummary>
{
    public Task<ChannelSummary> Handle(CreateChannelCommand request, CancellationToken cancellationToken)
    {
        return channels.CreateAsync(request.CreatorUserId, request.Name, request.Description, request.Type, cancellationToken);
    }
}

public sealed class JoinChannelCommandHandler(IChannelRepository channels) : IRequestHandler<JoinChannelCommand, ChannelMemberDto>
{
    public Task<ChannelMemberDto> Handle(JoinChannelCommand request, CancellationToken cancellationToken)
    {
        return channels.JoinAsync(request.ChannelId, request.UserId, cancellationToken);
    }
}

public sealed class SendChannelMessageCommandHandler(IChannelRepository channels) : IRequestHandler<SendChannelMessageCommand, ChannelMessageDto>
{
    public Task<ChannelMessageDto> Handle(SendChannelMessageCommand request, CancellationToken cancellationToken)
    {
        return channels.SendMessageAsync(request.ChannelId, request.SenderUserId, request.ClientMessageId, request.Body, cancellationToken);
    }
}
