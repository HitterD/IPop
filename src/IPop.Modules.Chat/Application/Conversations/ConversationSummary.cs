namespace IPop.Modules.Chat.Application.Conversations;

public sealed record ConversationSummary(
    Guid OtherUserId,
    string OtherDisplayName,
    string LastMessagePreview,
    DateTimeOffset LastMessageAt);
