using ImapClient.Contracts;

namespace ImapClient.Api.Services;

public interface IImapMailService
{
    Task<IReadOnlyList<MailFolderDto>> GetFoldersAsync(MailConnectionRequest connection, CancellationToken cancellationToken);

    Task<IReadOnlyList<MailMessageSummaryDto>> GetMessagesAsync(MailFolderQueryRequest request, CancellationToken cancellationToken);

    Task<MailMessageDetailDto?> GetMessageAsync(MailMessageLookupRequest request, CancellationToken cancellationToken);

    Task MarkAsReadAsync(MailMessageLookupRequest request, CancellationToken cancellationToken);

    Task MarkAsUnreadAsync(MailMessageLookupRequest request, CancellationToken cancellationToken);

    Task DeleteAsync(MailMessageLookupRequest request, CancellationToken cancellationToken);

    Task MoveAsync(MailMoveMessageRequest request, CancellationToken cancellationToken);
}