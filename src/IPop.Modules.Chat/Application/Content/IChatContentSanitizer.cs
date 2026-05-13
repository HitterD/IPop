namespace IPop.Modules.Chat.Application.Content;

public interface IChatContentSanitizer
{
    ChatContent Sanitize(string input);
}

public sealed record ChatContent(string Html, string Plain);
