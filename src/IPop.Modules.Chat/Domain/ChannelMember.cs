namespace IPop.Modules.Chat.Domain;

public sealed class ChannelMember
{
    private ChannelMember() { }

    public Guid Id { get; private set; }
    public Guid ChannelId { get; private set; }
    public Guid UserId { get; private set; }
    public ChannelMemberRole Role { get; private set; }
    public DateTimeOffset JoinedAt { get; private set; }

    public static ChannelMember Create(Guid channelId, Guid userId, ChannelMemberRole role, DateTimeOffset joinedAt)
    {
        return new ChannelMember
        {
            Id = Guid.NewGuid(),
            ChannelId = channelId,
            UserId = userId,
            Role = role,
            JoinedAt = joinedAt
        };
    }

    internal void Promote(ChannelMemberRole role)
    {
        Role = role;
    }

    internal void AttachToChannel(Guid channelId)
    {
        ChannelId = channelId;
    }
}
