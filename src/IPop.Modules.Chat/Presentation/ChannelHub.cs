using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using IPop.Modules.Chat.Application.Abstractions;
using IPop.Modules.Chat.Application.Channels;
using IPop.Shared.Auth;

namespace IPop.Modules.Chat.Presentation;

[Authorize(Policy = AuthorizationPolicies.RequireAuthenticated)]
public sealed class ChannelHub(IMediator mediator, IChannelRepository channels) : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = GetCurrentUserId();
        var cancellationToken = Context.ConnectionAborted;
        var summaries = await channels.ListAsync(userId, cancellationToken);
        foreach (var channel in summaries.Where(c => c.IsMember))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, ChannelGroup(channel.ChannelId), cancellationToken);
        }
        await base.OnConnectedAsync();
    }

    public async Task JoinChannel(Guid channelId)
    {
        var userId = GetCurrentUserId();
        await mediator.Send(new JoinChannelCommand(channelId, userId));
        await Groups.AddToGroupAsync(Context.ConnectionId, ChannelGroup(channelId), Context.ConnectionAborted);
        await Clients.Group(ChannelGroup(channelId)).SendAsync("ChannelMembershipChanged", channelId, userId, true, Context.ConnectionAborted);
    }

    public async Task LeaveChannel(Guid channelId)
    {
        var userId = GetCurrentUserId();
        await channels.LeaveAsync(channelId, userId, Context.ConnectionAborted);
        await Clients.Group(ChannelGroup(channelId)).SendAsync("ChannelMembershipChanged", channelId, userId, false, Context.ConnectionAborted);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, ChannelGroup(channelId), Context.ConnectionAborted);
    }

    public async Task SendChannelMessage(Guid channelId, Guid clientMessageId, string body)
    {
        var senderUserId = GetCurrentUserId();
        var message = await mediator.Send(new SendChannelMessageCommand(channelId, senderUserId, clientMessageId, body));
        await Clients.Group(ChannelGroup(channelId)).SendAsync("ChannelMessageReceived", message, Context.ConnectionAborted);
    }

    private Guid GetCurrentUserId()
    {
        var value = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId)
            ? userId
            : throw new HubException("Authenticated user id is missing.");
    }

    private static string ChannelGroup(Guid channelId) => $"channel:{channelId}";
}
