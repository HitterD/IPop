namespace IPop.Modules.Chat.Application.Channels;

public sealed record ChannelSummary(
    Guid ChannelId,
    string Name,
    string? Description,
    ChannelTypeDto Type,
    int MemberCount,
    string LastMessagePreview,
    DateTimeOffset LastMessageAt,
    bool IsMember);

public enum ChannelTypeDto
{
    Public = 0,
    Private = 1
}
