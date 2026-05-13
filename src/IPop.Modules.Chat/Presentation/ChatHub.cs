using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using IPop.Modules.Chat.Application.Abstractions;
using IPop.Modules.Chat.Application.Messages;
using IPop.Modules.Chat.Application.Presence;
using IPop.Shared.Auth;

namespace IPop.Modules.Chat.Presentation;

[Authorize(Policy = AuthorizationPolicies.RequireAuthenticated)]
public sealed class ChatHub(IMediator mediator, IPresenceService presence, IChatRepository repository) : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = GetCurrentUserId();
        var cancellationToken = Context.ConnectionAborted;
        await presence.SetAsync(userId, PresenceState.Online, cancellationToken);
        await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(userId), cancellationToken);

        var deliveredAt = DateTimeOffset.UtcNow;
        var deliveredMessages = await repository.MarkUndeliveredMessagesDeliveredAsync(userId, deliveredAt, cancellationToken);
        foreach (var message in deliveredMessages)
        {
            await Clients.Group(UserGroup(message.SenderUserId)).SendAsync("MessageDelivered", message.Id, message.DeliveredAt, cancellationToken);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await presence.ClearAsync(GetCurrentUserId(), CancellationToken.None);
        await base.OnDisconnectedAsync(exception);
    }

    public async Task SendDirectMessage(Guid recipientUserId, Guid clientMessageId, string body, Guid[] attachmentIds)
    {
        var senderUserId = GetCurrentUserId();
        var message = await mediator.Send(new SendDirectMessageCommand(senderUserId, recipientUserId, clientMessageId, body, attachmentIds));
        var recipientPresence = await presence.GetManyAsync([recipientUserId], Context.ConnectionAborted);
        if (recipientPresence.TryGetValue(recipientUserId, out var state) && state is PresenceState.Online or PresenceState.Away)
        {
            var deliveredAt = DateTimeOffset.UtcNow;
            await repository.MarkDeliveredAsync(message.Id, deliveredAt, Context.ConnectionAborted);
            message = message with { DeliveredAt = message.DeliveredAt ?? deliveredAt };
            await Clients.Group(UserGroup(senderUserId)).SendAsync("MessageDelivered", message.Id, message.DeliveredAt, Context.ConnectionAborted);
        }

        await Clients.Group(UserGroup(senderUserId)).SendAsync("DirectMessageReceived", message, Context.ConnectionAborted);
        if (recipientUserId != senderUserId)
        {
            await Clients.Group(UserGroup(recipientUserId)).SendAsync("DirectMessageReceived", message, Context.ConnectionAborted);
        }
    }

    public Task Heartbeat(PresenceState state)
    {
        return presence.SetAsync(GetCurrentUserId(), state, Context.ConnectionAborted);
    }

    public async Task MarkRead(Guid[] messageIds)
    {
        var currentUserId = GetCurrentUserId();
        var readAt = DateTimeOffset.UtcNow;
        var senderUserIds = await repository.MarkReadAsync(currentUserId, messageIds, readAt, Context.ConnectionAborted);
        foreach (var senderUserId in senderUserIds)
        {
            await Clients.Group(UserGroup(senderUserId)).SendAsync("MessagesRead", messageIds, readAt, Context.ConnectionAborted);
        }
    }

    public async Task NotifyTyping(Guid recipientUserId, bool isTyping)
    {
        var senderUserId = GetCurrentUserId();
        if (recipientUserId == senderUserId)
        {
            return;
        }
        await Clients.Group(UserGroup(recipientUserId)).SendAsync("TypingChanged", senderUserId, isTyping, Context.ConnectionAborted);
    }

    private Guid GetCurrentUserId()
    {
        var value = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId)
            ? userId
            : throw new HubException("Authenticated user id is missing.");
    }

    private static string UserGroup(Guid userId) => $"user:{userId}";
}
