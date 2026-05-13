using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;
using IPop.Modules.Chat.Application.Abstractions;
using IPop.Modules.Chat.Application.Im;
using IPop.Modules.Chat.Application.Notifications;
using IPop.Modules.Chat.Domain;
using IPop.Modules.Chat.Presentation;
using IPop.Shared.Auth;

namespace IPop.Modules.Chat.Presentation;

internal static class ImEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/im").RequireAuthorization(AuthorizationPolicies.RequireAuthenticated);

        group.MapGet("/", async (
            string? folder,
            HttpContext httpContext,
            IImRepository im,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(httpContext, out var userId)) return Results.Unauthorized();
            var parsed = Enum.TryParse<ImFolder>(folder ?? "Inbox", true, out var f) ? f : ImFolder.Inbox;
            var list = await im.ListAsync(userId, parsed, cancellationToken);
            return Results.Ok(list);
        });

        group.MapGet("/unread-count", async (
            HttpContext httpContext,
            IImRepository im,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(httpContext, out var userId)) return Results.Unauthorized();
            var count = await im.GetUnreadCountAsync(userId, cancellationToken);
            return Results.Ok(new { count });
        });

        group.MapGet("/{id:guid}", async (
            Guid id,
            HttpContext httpContext,
            IImRepository im,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(httpContext, out var userId)) return Results.Unauthorized();
            var detail = await im.GetDetailAsync(userId, id, cancellationToken);
            return detail is null ? Results.NotFound() : Results.Ok(detail);
        });

        group.MapPost("/", async (
            SendImRequest request,
            HttpContext httpContext,
            IMediator mediator,
            IImRepository im,
            IHubContext<DesktopNotificationHub> hub,
            CancellationToken cancellationToken) =>
        {
            if (!ChatCsrfGuard.IsRequestSameOrigin(httpContext) || !ChatCsrfGuard.HasIPopRequestHeader(httpContext))
                return Results.StatusCode(StatusCodes.Status403Forbidden);
            if (!TryGetUserId(httpContext, out var userId)) return Results.Unauthorized();

            try
            {
                var summary = await mediator.Send(new SendImCommand(
                    userId,
                    request.RecipientUserIds,
                    request.Subject,
                    request.Body,
                    request.ParentMessageId,
                    request.MessageType), cancellationToken);

                // Push desktop notification to each inbox recipient
                var recipientIds = await im.GetRecipientUserIdsAsync(summary.Id, cancellationToken);
                var senderName = summary.SenderDisplayName;
                var payload = new SignalRPayload(
                    summary.Id,
                    "IPop IM",
                    $"{senderName}: {summary.Subject} — {summary.BodyPlain}",
                    $"/im?messageId={summary.Id}");
                foreach (var recipientId in recipientIds)
                {
                    await hub.Clients.Group(DesktopNotificationHub.DesktopGroup(recipientId))
                        .SendAsync("ReceiveNotification", payload, cancellationToken);
                }

                return Results.Ok(summary);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }).DisableAntiforgery();

        group.MapPost("/{id:guid}/read", async (
            Guid id,
            HttpContext httpContext,
            IImRepository im,
            CancellationToken cancellationToken) =>
        {
            if (!ChatCsrfGuard.IsRequestSameOrigin(httpContext) || !ChatCsrfGuard.HasIPopRequestHeader(httpContext))
                return Results.StatusCode(StatusCodes.Status403Forbidden);
            if (!TryGetUserId(httpContext, out var userId)) return Results.Unauthorized();
            await im.MarkReadAsync(userId, id, DateTimeOffset.UtcNow, cancellationToken);
            return Results.Ok();
        }).DisableAntiforgery();

        group.MapPost("/delete", async (
            DeleteImRequest request,
            HttpContext httpContext,
            IImRepository im,
            CancellationToken cancellationToken) =>
        {
            if (!ChatCsrfGuard.IsRequestSameOrigin(httpContext) || !ChatCsrfGuard.HasIPopRequestHeader(httpContext))
                return Results.StatusCode(StatusCodes.Status403Forbidden);
            if (!TryGetUserId(httpContext, out var userId)) return Results.Unauthorized();
            await im.DeleteAsync(userId, request.MessageIds, cancellationToken);
            return Results.Ok();
        }).DisableAntiforgery();
    }

    private static bool TryGetUserId(HttpContext httpContext, out Guid userId)
    {
        var value = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out userId);
    }

    public sealed record SendImRequest(
        IReadOnlyList<Guid> RecipientUserIds,
        string Subject,
        string Body,
        Guid? ParentMessageId,
        ImMessageType MessageType);

    public sealed record DeleteImRequest(IReadOnlyList<Guid> MessageIds);
}
