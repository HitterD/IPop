namespace IPop.Modules.Chat.Domain;

public sealed class ChannelMessage
{
    private ChannelMessage() { }

    public Guid Id { get; private set; }
    public Guid ChannelId { get; private set; }
    public Guid SenderUserId { get; private set; }
    public Guid ClientMessageId { get; private set; }
    public string Body { get; private set; } = string.Empty;
    public string BodyPlain { get; private set; } = string.Empty;
    public MessageType MessageType { get; private set; }
    public DateTimeOffset SentAt { get; private set; }

    public static ChannelMessage Create(Guid channelId, Guid senderUserId, Guid clientMessageId, string body, string bodyPlain, MessageType messageType, DateTimeOffset now)
    {
        if (channelId == Guid.Empty)
        {
            throw new ArgumentException("Channel id is required.", nameof(channelId));
        }
        if (senderUserId == Guid.Empty)
        {
            throw new ArgumentException("Sender user id is required.", nameof(senderUserId));
        }
        if (clientMessageId == Guid.Empty)
        {
            throw new ArgumentException("Client message id is required.", nameof(clientMessageId));
        }
        if (string.IsNullOrWhiteSpace(body))
        {
            throw new ArgumentException("Message body is required.", nameof(body));
        }
        var trimmedBody = body.Trim();
        return new ChannelMessage
        {
            Id = Guid.NewGuid(),
            ChannelId = channelId,
            SenderUserId = senderUserId,
            ClientMessageId = clientMessageId,
            Body = trimmedBody,
            BodyPlain = string.IsNullOrWhiteSpace(bodyPlain) ? trimmedBody : bodyPlain.Trim(),
            MessageType = messageType,
            SentAt = now
        };
    }
}
