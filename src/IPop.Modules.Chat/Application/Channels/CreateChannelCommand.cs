using MediatR;

namespace IPop.Modules.Chat.Application.Channels;

public sealed record CreateChannelCommand(
    Guid CreatorUserId,
    string Name,
    string? Description,
    ChannelTypeDto Type) : IRequest<ChannelSummary>;
