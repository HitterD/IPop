using MediatR;

namespace IPop.Modules.Chat.Application.Channels;

public sealed record JoinChannelCommand(Guid ChannelId, Guid UserId) : IRequest<ChannelMemberDto>;
