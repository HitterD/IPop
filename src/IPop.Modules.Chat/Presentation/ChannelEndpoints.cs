using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Builder;
using IPop.Modules.Chat.Application.Abstractions;
using IPop.Modules.Chat.Application.Channels;
using IPop.Shared.Auth;

namespace IPop.Modules.Chat.Presentation;

internal static class ChannelEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/channels").RequireAuthorization(AuthorizationPolicies.RequireAuthenticated);

        group.MapGet("/", async (
            HttpContext httpContext,
            IChannelRepository channels,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(httpContext, out var userId))
            {
                return Results.Unauthorized();
            }
            var list = await channels.ListAsync(userId, cancellationToken);
            return Results.Ok(list);
        });

        group.MapGet("/{id:guid}/messages", async (
            Guid id,
            HttpContext httpContext,
            IChannelRepository channels,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(httpContext, out var userId))
            {
                return Results.Unauthorized();
            }
            try
            {
                var messages = await channels.ListMessagesAsync(id, userId, cancellationToken);
                return Results.Ok(messages);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Json(new { error = ex.Message }, statusCode: StatusCodes.Status403Forbidden);
            }
        });

        group.MapGet("/{id:guid}/members", async (
            Guid id,
            HttpContext httpContext,
            IChannelRepository channels,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(httpContext, out var userId))
            {
                return Results.Unauthorized();
            }
            var members = await channels.ListMembersAsync(id, userId, cancellationToken);
            return Results.Ok(members);
        });

        group.MapPost("/", async (
            CreateChannelRequest request,
            HttpContext httpContext,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            if (!ChatCsrfGuard.IsRequestSameOrigin(httpContext) || !ChatCsrfGuard.HasIPopRequestHeader(httpContext))
            {
                return Results.StatusCode(StatusCodes.Status403Forbidden);
            }
            if (!TryGetUserId(httpContext, out var userId))
            {
                return Results.Unauthorized();
            }
            try
            {
                var summary = await mediator.Send(new CreateChannelCommand(userId, request.Name, request.Description, request.Type), cancellationToken);
                return Results.Ok(summary);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        group.MapPost("/{id:guid}/join", async (
            Guid id,
            HttpContext httpContext,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            if (!ChatCsrfGuard.IsRequestSameOrigin(httpContext) || !ChatCsrfGuard.HasIPopRequestHeader(httpContext))
            {
                return Results.StatusCode(StatusCodes.Status403Forbidden);
            }
            if (!TryGetUserId(httpContext, out var userId))
            {
                return Results.Unauthorized();
            }
            try
            {
                var member = await mediator.Send(new JoinChannelCommand(id, userId), cancellationToken);
                return Results.Ok(member);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });
    }

    private static bool TryGetUserId(HttpContext httpContext, out Guid userId)
    {
        var value = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out userId);
    }

    public sealed record CreateChannelRequest(string Name, string? Description, ChannelTypeDto Type);
}
