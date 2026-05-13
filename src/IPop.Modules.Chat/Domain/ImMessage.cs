namespace IPop.Modules.Chat.Domain;

public sealed class ImMessage
{
    public const int SubjectMaxLength = 200;
    public const int BodyMaxLength = 16000;
    public const int BodyPlainMaxLength = 400;

    private readonly List<ImRecipient> _recipients = new();
    private readonly List<ImAttachment> _attachments = new();

    private ImMessage() { }

    public Guid Id { get; private set; }
    public string Subject { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public string BodyPlain { get; private set; } = string.Empty;
    public Guid SenderUserId { get; private set; }
    public Guid? ParentMessageId { get; private set; }
    public ImMessageType MessageType { get; private set; }
    public DateTimeOffset SentAt { get; private set; }
    public IReadOnlyCollection<ImRecipient> Recipients => _recipients;
    public IReadOnlyCollection<ImAttachment> Attachments => _attachments;

    public static ImMessage Create(
        string subject,
        string body,
        string bodyPlain,
        Guid senderUserId,
        IReadOnlyCollection<Guid> recipientUserIds,
        Guid? parentMessageId,
        ImMessageType messageType,
        DateTimeOffset now)
    {
        if (senderUserId == Guid.Empty)
            throw new ArgumentException("Sender user id is required.", nameof(senderUserId));
        if (string.IsNullOrWhiteSpace(subject))
            throw new ArgumentException("Subject is required.", nameof(subject));
        if (subject.Trim().Length > SubjectMaxLength)
            throw new ArgumentException($"Subject cannot exceed {SubjectMaxLength} characters.", nameof(subject));
        if (string.IsNullOrWhiteSpace(body))
            throw new ArgumentException("Body is required.", nameof(body));
        if (body.Trim().Length > BodyMaxLength)
            throw new ArgumentException($"Body cannot exceed {BodyMaxLength} characters.", nameof(body));
        if (recipientUserIds.Count == 0)
            throw new ArgumentException("At least one recipient is required.", nameof(recipientUserIds));

        var trimmedSubject = subject.Trim();
        var trimmedBody = body.Trim();
        var trimmedPlain = string.IsNullOrWhiteSpace(bodyPlain)
            ? Truncate(trimmedBody, BodyPlainMaxLength)
            : Truncate(bodyPlain.Trim(), BodyPlainMaxLength);

        var message = new ImMessage
        {
            Id = Guid.NewGuid(),
            Subject = trimmedSubject,
            Body = trimmedBody,
            BodyPlain = trimmedPlain,
            SenderUserId = senderUserId,
            ParentMessageId = parentMessageId,
            MessageType = messageType,
            SentAt = now
        };

        // Sender gets Sent row
        message._recipients.Add(ImRecipient.CreateSent(message.Id, senderUserId));

        // Each recipient gets Inbox row (skip sender if included)
        foreach (var recipientId in recipientUserIds.Distinct())
        {
            if (recipientId == Guid.Empty)
                throw new ArgumentException("Recipient user id cannot be empty.", nameof(recipientUserIds));
            if (recipientId != senderUserId)
                message._recipients.Add(ImRecipient.CreateInbox(message.Id, recipientId));
        }

        return message;
    }

    private static string Truncate(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength) return text;
        return text[..maxLength];
    }
}
