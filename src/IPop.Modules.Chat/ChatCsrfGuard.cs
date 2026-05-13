using Microsoft.AspNetCore.Http;

namespace IPop.Modules.Chat;

public static class ChatCsrfGuard
{
    public const string RequestHeaderName = "X-Requested-With";
    public const string RequestHeaderValue = "IPopChat";

    public static bool IsRequestSameOrigin(HttpContext context)
    {
        var request = context.Request;
        var expectedHost = request.Host.ToString();
        var expectedScheme = request.Scheme;

        if (request.Headers.TryGetValue("Origin", out var origin) && origin.Count > 0 && !string.IsNullOrEmpty(origin[0]))
        {
            return Uri.TryCreate(origin[0], UriKind.Absolute, out var originUri)
                && string.Equals(originUri.Host, request.Host.Host, StringComparison.OrdinalIgnoreCase)
                && (request.Host.Port is null || originUri.Port == request.Host.Port);
        }

        if (request.Headers.TryGetValue("Referer", out var referer) && referer.Count > 0 && !string.IsNullOrEmpty(referer[0]))
        {
            if (Uri.TryCreate(referer[0], UriKind.Absolute, out var refererUri))
            {
                return string.Equals(refererUri.Host, request.Host.Host, StringComparison.OrdinalIgnoreCase)
                    && (request.Host.Port is null || refererUri.Port == request.Host.Port)
                    && string.Equals(refererUri.Scheme, expectedScheme, StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }

        return false;
    }

    public static bool HasIPopRequestHeader(HttpContext context)
    {
        return context.Request.Headers.TryGetValue(RequestHeaderName, out var values)
            && values.Count > 0
            && string.Equals(values[0], RequestHeaderValue, StringComparison.Ordinal);
    }
}
