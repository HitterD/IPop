namespace IPop.Modules.Chat.Domain;

public sealed class Channel
{
    public const int NameMaxLength = 80;
    public const int DescriptionMaxLength = 280;
    public const int MemberCapacity = 1000;

    private readonly List<ChannelMember> _members = new();

    private Channel() { }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string NameKey { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public ChannelType Type { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset LastMessageAt { get; private set; }
    public IReadOnlyCollection<ChannelMember> Members => _members;

    public static Channel Create(string name, string? description, ChannelType type, Guid createdByUserId, DateTimeOffset now)
    {
        if (createdByUserId == Guid.Empty)
        {
            throw new ArgumentException("Creator user id is required.", nameof(createdByUserId));
        }
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Channel name is required.", nameof(name));
        }
        var trimmed = name.Trim();
        if (trimmed.Length > NameMaxLength)
        {
            throw new ArgumentException($"Channel name cannot exceed {NameMaxLength} characters.", nameof(name));
        }
        var normalizedDescription = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        if (normalizedDescription is not null && normalizedDescription.Length > DescriptionMaxLength)
        {
            throw new ArgumentException($"Channel description cannot exceed {DescriptionMaxLength} characters.", nameof(description));
        }

        var channel = new Channel
        {
            Id = Guid.NewGuid(),
            Name = trimmed,
            NameKey = trimmed.ToLowerInvariant(),
            Description = normalizedDescription,
            Type = type,
            CreatedByUserId = createdByUserId,
            CreatedAt = now,
            LastMessageAt = now
        };
        channel._members.Add(ChannelMember.Create(channel.Id, createdByUserId, ChannelMemberRole.Admin, now));
        return channel;
    }

    public ChannelMember AddMember(Guid userId, ChannelMemberRole role, DateTimeOffset now)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User id is required.", nameof(userId));
        }
        if (_members.Any(x => x.UserId == userId))
        {
            throw new InvalidOperationException("User is already a member of this channel.");
        }
        if (_members.Count >= MemberCapacity)
        {
            throw new InvalidOperationException("Channel member capacity exceeded.");
        }
        var member = ChannelMember.Create(Id, userId, role, now);
        _members.Add(member);
        return member;
    }

    public void RemoveMember(Guid userId)
    {
        var member = _members.SingleOrDefault(x => x.UserId == userId);
        if (member is null)
        {
            return;
        }
        if (member.UserId == CreatedByUserId && _members.Count(x => x.Role == ChannelMemberRole.Admin) <= 1)
        {
            throw new InvalidOperationException("Cannot remove the last admin of a channel.");
        }
        _members.Remove(member);
    }

    public bool IsMember(Guid userId) => _members.Any(x => x.UserId == userId);

    public void Touch(DateTimeOffset at)
    {
        LastMessageAt = at;
    }
}
