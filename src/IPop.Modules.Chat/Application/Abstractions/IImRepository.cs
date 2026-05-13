using IPop.Modules.Chat.Application.Im;
using IPop.Modules.Chat.Domain;

namespace IPop.Modules.Chat.Application.Abstractions;

public interface IImRepository
{
    Task<IReadOnlyList<ImMessageSummary>> ListAsync(Guid currentUserId, ImFolder folder, CancellationToken cancellationToken);
    Task<ImMessageDetail?> GetDetailAsync(Guid currentUserId, Guid messageId, CancellationToken cancellationToken);
    Task<ImMessageSummary> SendAsync(Guid senderUserId, IReadOnlyList<Guid> recipientUserIds, string subject, string body, Guid? parentMessageId, ImMessageType messageType, CancellationToken cancellationToken);
    Task MarkReadAsync(Guid currentUserId, Guid messageId, DateTimeOffset readAt, CancellationToken cancellationToken);
    Task DeleteAsync(Guid currentUserId, IReadOnlyList<Guid> messageIds, CancellationToken cancellationToken);
    Task<int> GetUnreadCountAsync(Guid currentUserId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Guid>> GetRecipientUserIdsAsync(Guid messageId, CancellationToken cancellationToken);
}
