namespace IPop.Modules.Chat.Application.Channels;

public sealed record ChannelMemberDto(
    Guid UserId,
    string DisplayName,
    string ChannelRole,
    DateTimeOffset JoinedAt);
