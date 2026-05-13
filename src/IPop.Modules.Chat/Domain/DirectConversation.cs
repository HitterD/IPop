namespace IPop.Modules.Chat.Domain;

public sealed class DirectConversation
{
    private DirectConversation() { }

    public Guid Id { get; private set; }
    public Guid UserAId { get; private set; }
    public Guid UserBId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset LastMessageAt { get; private set; }

    public static DirectConversation Create(Guid firstUserId, Guid secondUserId, DateTimeOffset now)
    {
        if (firstUserId == secondUserId)
        {
            throw new ArgumentException("Direct conversation requires two different users.", nameof(firstUserId));
        }

        var userA = firstUserId.CompareTo(secondUserId) < 0 ? firstUserId : secondUserId;
        var userB = firstUserId.CompareTo(secondUserId) < 0 ? secondUserId : firstUserId;

        return new DirectConversation
        {
            Id = Guid.NewGuid(),
            UserAId = userA,
            UserBId = userB,
            CreatedAt = now,
            LastMessageAt = now
        };
    }

    public void Touch(DateTimeOffset at)
    {
        LastMessageAt = at;
    }
}
