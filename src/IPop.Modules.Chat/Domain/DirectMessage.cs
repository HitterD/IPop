namespace IPop.Modules.Chat.Domain;

public sealed class DirectMessage
{
    private DirectMessage() { }

    private readonly List<DirectAttachment> _attachments = new();

    public Guid Id { get; private set; }
    public Guid ConversationId { get; private set; }
    public Guid SenderUserId { get; private set; }
    public Guid RecipientUserId { get; private set; }
    public Guid ClientMessageId { get; private set; }
    public string Body { get; private set; } = string.Empty;
    public string BodyPlain { get; private set; } = string.Empty;
    public MessageType MessageType { get; private set; }
    public DateTimeOffset SentAt { get; private set; }
    public DateTimeOffset? DeliveredAt { get; private set; }
    public DateTimeOffset? ReadAt { get; private set; }
    public DirectConversation? Conversation { get; private set; }
    public IReadOnlyCollection<DirectAttachment> Attachments => _attachments;

    public static DirectMessage Create(Guid conversationId, Guid senderUserId, Guid recipientUserId, Guid clientMessageId, string body, string bodyPlain, MessageType messageType, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            throw new ArgumentException("Message body is required.", nameof(body));
        }

        var trimmedBody = body.Trim();
        return new DirectMessage
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            SenderUserId = senderUserId,
            RecipientUserId = recipientUserId,
            ClientMessageId = clientMessageId,
            Body = trimmedBody,
            BodyPlain = string.IsNullOrWhiteSpace(bodyPlain) ? trimmedBody : bodyPlain.Trim(),
            MessageType = messageType,
            SentAt = now
        };
    }

    public void MarkRead(DateTimeOffset at)
    {
        ReadAt ??= at;
    }

    public void MarkDelivered(DateTimeOffset at) => DeliveredAt ??= at;

    public void AddAttachment(DirectAttachment attachment)
    {
        attachment.AttachToMessage(Id);
        _attachments.Add(attachment);
    }
}
