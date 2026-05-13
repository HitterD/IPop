using MediatR;
using IPop.Modules.Chat.Application.Im;
using IPop.Modules.Chat.Domain;

namespace IPop.Modules.Chat.Application.Im;

public sealed record SendImCommand(
    Guid SenderUserId,
    IReadOnlyList<Guid> RecipientUserIds,
    string Subject,
    string Body,
    Guid? ParentMessageId,
    ImMessageType MessageType) : IRequest<ImMessageSummary>;

public sealed record MarkImReadCommand(
    Guid CurrentUserId,
    Guid MessageId) : IRequest;

public sealed record DeleteImCommand(
    Guid CurrentUserId,
    IReadOnlyList<Guid> MessageIds) : IRequest;
