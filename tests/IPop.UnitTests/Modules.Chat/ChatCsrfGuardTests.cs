using FluentAssertions;
using Microsoft.AspNetCore.Http;
using IPop.Modules.Chat;
using Xunit;

namespace IPop.UnitTests.Modules.Chat;

public sealed class ChatCsrfGuardTests
{
    [Fact]
    public void IsRequestSameOrigin_OriginMatchesHost_Allows()
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Scheme = "https";
        ctx.Request.Host = new HostString("ipop.local");
        ctx.Request.Headers.Origin = "https://ipop.local";

        ChatCsrfGuard.IsRequestSameOrigin(ctx).Should().BeTrue();
    }

    [Fact]
    public void IsRequestSameOrigin_OriginMismatch_Rejects()
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Scheme = "https";
        ctx.Request.Host = new HostString("ipop.local");
        ctx.Request.Headers.Origin = "https://attacker.example";

        ChatCsrfGuard.IsRequestSameOrigin(ctx).Should().BeFalse();
    }

    [Fact]
    public void IsRequestSameOrigin_NoOriginNoReferer_Rejects()
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Scheme = "https";
        ctx.Request.Host = new HostString("ipop.local");

        ChatCsrfGuard.IsRequestSameOrigin(ctx).Should().BeFalse();
    }

    [Fact]
    public void IsRequestSameOrigin_RefererMatch_Allows()
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Scheme = "https";
        ctx.Request.Host = new HostString("ipop.local");
        ctx.Request.Headers.Referer = "https://ipop.local/chats";

        ChatCsrfGuard.IsRequestSameOrigin(ctx).Should().BeTrue();
    }

    [Fact]
    public void HasIPopRequestHeader_Missing_Rejects()
    {
        var ctx = new DefaultHttpContext();
        ChatCsrfGuard.HasIPopRequestHeader(ctx).Should().BeFalse();
    }

    [Fact]
    public void HasIPopRequestHeader_Present_Allows()
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Headers["X-Requested-With"] = "IPopChat";
        ChatCsrfGuard.HasIPopRequestHeader(ctx).Should().BeTrue();
    }
}
