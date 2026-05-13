using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using IPop.Modules.Chat.Application.Notifications;
using IPop.Shared.Auth;

namespace IPop.Modules.Chat.Presentation;

[Authorize(Policy = AuthorizationPolicies.RequireAuthenticated)]
public sealed class DesktopNotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = GetCurrentUserId();
        await Groups.AddToGroupAsync(Context.ConnectionId, DesktopGroup(userId), Context.ConnectionAborted);
        await base.OnConnectedAsync();
    }

    private Guid GetCurrentUserId()
    {
        var value = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId)
            ? userId
            : throw new HubException("Authenticated user id is missing.");
    }

    public static string DesktopGroup(Guid userId) => $"desktop:{userId}";
}
