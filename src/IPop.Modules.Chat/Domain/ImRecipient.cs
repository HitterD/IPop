namespace IPop.Modules.Chat.Domain;

public sealed class ImRecipient
{
    private ImRecipient() { }

    public Guid Id { get; private set; }
    public Guid MessageId { get; private set; }
    public Guid UserId { get; private set; }
    public ImFolder Folder { get; private set; }
    public bool IsRead { get; private set; }
    public DateTimeOffset? ReadAt { get; private set; }

    public static ImRecipient CreateInbox(Guid messageId, Guid userId)
    {
        if (messageId == Guid.Empty) throw new ArgumentException("Message id is required.", nameof(messageId));
        if (userId == Guid.Empty) throw new ArgumentException("User id is required.", nameof(userId));
        return new ImRecipient { Id = Guid.NewGuid(), MessageId = messageId, UserId = userId, Folder = ImFolder.Inbox, IsRead = false };
    }

    public static ImRecipient CreateSent(Guid messageId, Guid userId)
    {
        if (messageId == Guid.Empty) throw new ArgumentException("Message id is required.", nameof(messageId));
        if (userId == Guid.Empty) throw new ArgumentException("User id is required.", nameof(userId));
        return new ImRecipient { Id = Guid.NewGuid(), MessageId = messageId, UserId = userId, Folder = ImFolder.Sent, IsRead = true };
    }

    public void MarkRead(DateTimeOffset at)
    {
        if (IsRead) return;
        IsRead = true;
        ReadAt = at;
    }

    public void MoveToDeleted()
    {
        Folder = ImFolder.Deleted;
    }

    public void Restore(ImFolder targetFolder)
    {
        if (Folder != ImFolder.Deleted) return;
        Folder = targetFolder;
    }
}
