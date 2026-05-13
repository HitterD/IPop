using System.Net;
using System.Text.RegularExpressions;
using Ganss.Xss;
using IPop.Modules.Chat.Application.Content;

namespace IPop.Infrastructure.Chat;

public sealed class HtmlAgilityChatContentSanitizer : IChatContentSanitizer
{
    private const int MaxPlainTextLength = 400;

    private static readonly string[] AllowedTags =
    [
        "b",
        "strong",
        "i",
        "em",
        "u",
        "a",
        "br",
        "p",
        "span",
        "ul",
        "ol",
        "li"
    ];

    private static readonly string[] AllowedAttributes =
    [
        "href",
        "class",
        "data-user-id"
    ];

    private static readonly string[] AllowedSchemes =
    [
        "http",
        "https",
        "mailto"
    ];

    private readonly HtmlSanitizer _sanitizer;

    public HtmlAgilityChatContentSanitizer()
    {
        _sanitizer = new HtmlSanitizer();

        _sanitizer.AllowedTags.Clear();
        foreach (var tag in AllowedTags)
        {
            _sanitizer.AllowedTags.Add(tag);
        }

        _sanitizer.AllowedAttributes.Clear();
        foreach (var attribute in AllowedAttributes)
        {
            _sanitizer.AllowedAttributes.Add(attribute);
        }

        _sanitizer.AllowedSchemes.Clear();
        foreach (var scheme in AllowedSchemes)
        {
            _sanitizer.AllowedSchemes.Add(scheme);
        }
    }

    public ChatContent Sanitize(string input)
    {
        var html = _sanitizer.Sanitize(input ?? string.Empty);
        var plainWithoutTags = Regex.Replace(html, "<[^>]+>", " ");
        var decodedPlain = WebUtility.HtmlDecode(plainWithoutTags);
        var plain = Regex.Replace(decodedPlain, "\\s+", " ").Trim();

        if (plain.Length > MaxPlainTextLength)
        {
            plain = plain[..MaxPlainTextLength];
        }

        return new ChatContent(html, plain);
    }
}
