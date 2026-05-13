using FluentAssertions;
using IPop.Infrastructure.Chat;
using Xunit;

namespace IPop.UnitTests.Modules.Chat;

public sealed class ChatContentSanitizerTests
{
    [Fact]
    public void Sanitize_StripsScriptAndEventHandlers()
    {
        var sanitizer = new HtmlAgilityChatContentSanitizer();

        var result = sanitizer.Sanitize("<p onclick='x()'>Hi<script>alert(1)</script></p>");

        result.Html.Should().NotContain("script").And.NotContain("onclick");
        result.Plain.Should().Be("Hi");
    }

    [Fact]
    public void Sanitize_KeepsMentionSpanAndSafeLink()
    {
        var sanitizer = new HtmlAgilityChatContentSanitizer();
        var id = Guid.NewGuid();

        var result = sanitizer.Sanitize($"<a href='https://sja.local'>link</a><span class='ipop-mention' data-user-id='{id}'>@Alice</span>");

        result.Html.Should().Contain("href=\"https://sja.local\"").And.Contain("data-user-id");
        result.Plain.Should().Contain("link").And.Contain("@Alice");
    }

    [Fact]
    public void Sanitize_RemovesJavascriptScheme()
    {
        var sanitizer = new HtmlAgilityChatContentSanitizer();

        var result = sanitizer.Sanitize("<a href='javascript:alert(1)'>bad</a>");

        result.Html.ToLowerInvariant().Should().NotContain("javascript");
        result.Plain.Should().Be("bad");
    }

    [Fact]
    public void Sanitize_EmptyInput_ReturnsEmptyHtmlAndPlain()
    {
        var sanitizer = new HtmlAgilityChatContentSanitizer();

        var result = sanitizer.Sanitize(string.Empty);

        result.Html.Should().BeEmpty();
        result.Plain.Should().BeEmpty();
    }
}
