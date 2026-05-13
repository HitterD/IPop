using IPop.Modules.Chat.Domain;

namespace IPop.Modules.Chat.Application.Channels;

public sealed record ChannelMessageDto(
    Guid Id,
    Guid ChannelId,
    Guid SenderUserId,
    string SenderDisplayName,
    Guid ClientMessageId,
    string Body,
    string BodyPlain,
    MessageType MessageType,
    DateTimeOffset SentAt);
