using IPop.Modules.Chat.Application.Channels;

namespace IPop.Modules.Chat.Application.Abstractions;

public interface IChannelRepository
{
    Task<IReadOnlyList<ChannelSummary>> ListAsync(Guid currentUserId, CancellationToken cancellationToken);
    Task<ChannelSummary?> GetAsync(Guid channelId, Guid currentUserId, CancellationToken cancellationToken);
    Task<ChannelSummary> CreateAsync(Guid creatorUserId, string name, string? description, ChannelTypeDto type, CancellationToken cancellationToken);
    Task<ChannelMemberDto> JoinAsync(Guid channelId, Guid userId, CancellationToken cancellationToken);
    Task LeaveAsync(Guid channelId, Guid userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<ChannelMemberDto>> ListMembersAsync(Guid channelId, Guid currentUserId, CancellationToken cancellationToken);
    Task<IReadOnlyList<ChannelMessageDto>> ListMessagesAsync(Guid channelId, Guid currentUserId, CancellationToken cancellationToken);
    Task<ChannelMessageDto> SendMessageAsync(Guid channelId, Guid senderUserId, Guid clientMessageId, string body, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Guid>> GetMemberUserIdsAsync(Guid channelId, CancellationToken cancellationToken);
}
