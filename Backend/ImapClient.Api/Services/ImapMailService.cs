using System.Net;
using System.Text.RegularExpressions;
using ImapClient.Contracts;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using MimeKit;
using MailKitImapClient = MailKit.Net.Imap.ImapClient;

namespace ImapClient.Api.Services;

public sealed class ImapMailService : IImapMailService
{
    private static readonly Regex HtmlTagRegex = new("<.*?>", RegexOptions.Compiled | RegexOptions.Singleline);

    public async Task<IReadOnlyList<MailFolderDto>> GetFoldersAsync(MailConnectionRequest connection, CancellationToken cancellationToken)
    {
        using var client = await ConnectAsync(connection, cancellationToken);
        var folders = new List<MailFolderDto>();

        await AddFolderAsync(client.Inbox, folders, cancellationToken);

        foreach (var specialFolder in new[] { SpecialFolder.Sent, SpecialFolder.Drafts, SpecialFolder.Archive, SpecialFolder.Junk, SpecialFolder.Trash })
        {
            var folder = TryGetSpecialFolder(client, specialFolder);
            if (folder is null)
            {
                continue;
            }

            await AddFolderAsync(folder, folders, cancellationToken);
        }

        return folders;
    }

    public async Task<IReadOnlyList<MailMessageSummaryDto>> GetMessagesAsync(MailFolderQueryRequest request, CancellationToken cancellationToken)
    {
        using var client = await ConnectAsync(request.Connection, cancellationToken);
        var folder = await OpenFolderAsync(client, request.FolderName, FolderAccess.ReadOnly, cancellationToken);
        var uids = await folder.SearchAsync(SearchQuery.All, cancellationToken);
        var seenUids = await folder.SearchAsync(SearchQuery.Seen, cancellationToken);
        var take = Math.Clamp(request.Take, 1, 100);
        var messages = new List<MailMessageSummaryDto>(take);

        foreach (var uid in uids.OrderByDescending(id => id.Id).Take(take))
        {
            var message = await folder.GetMessageAsync(uid, cancellationToken);
            messages.Add(ToSummary(folder.FullName, uid, message, seenUids.Contains(uid)));
        }

        return messages;
    }

    public async Task<MailMessageDetailDto?> GetMessageAsync(MailMessageLookupRequest request, CancellationToken cancellationToken)
    {
        using var client = await ConnectAsync(request.Connection, cancellationToken);
        var folder = await OpenFolderAsync(client, request.FolderName, FolderAccess.ReadOnly, cancellationToken);
        var uid = new UniqueId(request.UniqueId);
        var seenUids = await folder.SearchAsync(SearchQuery.Seen, cancellationToken);
        var message = await folder.GetMessageAsync(uid, cancellationToken);

        if (message is null)
        {
            return null;
        }

        return ToDetail(folder.FullName, uid, message, seenUids.Contains(uid));
    }

    public async Task MarkAsReadAsync(MailMessageLookupRequest request, CancellationToken cancellationToken)
    {
        await UpdateFlagsAsync(request, MessageFlags.Seen, add: true, cancellationToken);
    }

    public async Task MarkAsUnreadAsync(MailMessageLookupRequest request, CancellationToken cancellationToken)
    {
        await UpdateFlagsAsync(request, MessageFlags.Seen, add: false, cancellationToken);
    }

    public async Task DeleteAsync(MailMessageLookupRequest request, CancellationToken cancellationToken)
    {
        using var client = await ConnectAsync(request.Connection, cancellationToken);
        var sourceFolder = await OpenFolderAsync(client, request.FolderName, FolderAccess.ReadWrite, cancellationToken);
        var trashFolder = TryGetSpecialFolder(client, SpecialFolder.Trash);

        if (trashFolder is not null)
        {
            if (!trashFolder.IsOpen)
            {
                await trashFolder.OpenAsync(FolderAccess.ReadWrite, cancellationToken);
            }

            await sourceFolder.MoveToAsync(new UniqueId(request.UniqueId), trashFolder, cancellationToken);
            return;
        }

        await sourceFolder.AddFlagsAsync(new UniqueId(request.UniqueId), MessageFlags.Deleted, true, cancellationToken);
        await sourceFolder.ExpungeAsync(cancellationToken);
    }

    public async Task MoveAsync(MailMoveMessageRequest request, CancellationToken cancellationToken)
    {
        using var client = await ConnectAsync(request.Connection, cancellationToken);
        var sourceFolder = await OpenFolderAsync(client, request.SourceFolderName, FolderAccess.ReadWrite, cancellationToken);
        var destinationFolder = await OpenFolderAsync(client, request.DestinationFolderName, FolderAccess.ReadWrite, cancellationToken);
        await sourceFolder.MoveToAsync(new UniqueId(request.UniqueId), destinationFolder, cancellationToken);
    }

    private static async Task UpdateFlagsAsync(MailMessageLookupRequest request, MessageFlags flags, bool add, CancellationToken cancellationToken)
    {
        using var client = await ConnectAsync(request.Connection, cancellationToken);
        var folder = await OpenFolderAsync(client, request.FolderName, FolderAccess.ReadWrite, cancellationToken);
        var uid = new UniqueId(request.UniqueId);

        if (add)
        {
            await folder.AddFlagsAsync(uid, flags, true, cancellationToken);
            return;
        }

        await folder.RemoveFlagsAsync(uid, flags, true, cancellationToken);
    }

    private static async Task<MailKitImapClient> ConnectAsync(MailConnectionRequest connection, CancellationToken cancellationToken)
    {
        var client = new MailKitImapClient();
        await client.ConnectAsync(connection.Host, connection.Port, connection.UseSsl, cancellationToken);

        if (connection.UseOAuth2)
        {
            if (string.IsNullOrWhiteSpace(connection.AccessToken))
            {
                throw new InvalidOperationException("An access token is required when OAuth2 is enabled.");
            }

            await client.AuthenticateAsync(new SaslMechanismOAuth2(connection.Username, connection.AccessToken), cancellationToken);
            return client;
        }

        if (string.IsNullOrWhiteSpace(connection.Password))
        {
            throw new InvalidOperationException("A password is required when OAuth2 is disabled.");
        }

        await client.AuthenticateAsync(connection.Username, connection.Password, cancellationToken);
        return client;
    }

    private static async Task<IMailFolder> OpenFolderAsync(MailKitImapClient client, string folderName, FolderAccess access, CancellationToken cancellationToken)
    {
        var folder = ResolveFolder(client, folderName);
        await folder.OpenAsync(access, cancellationToken);
        return folder;
    }

    private static IMailFolder ResolveFolder(MailKitImapClient client, string folderName)
    {
        if (string.Equals(folderName, "INBOX", StringComparison.OrdinalIgnoreCase))
        {
            return client.Inbox;
        }

        if (Enum.TryParse<SpecialFolder>(folderName, true, out var specialFolder))
        {
            var resolved = TryGetSpecialFolder(client, specialFolder);
            if (resolved is not null)
            {
                return resolved;
            }
        }

        return client.GetFolder(folderName);
    }

    private static IMailFolder? TryGetSpecialFolder(MailKitImapClient client, SpecialFolder specialFolder)
    {
        try
        {
            return client.GetFolder(specialFolder);
        }
        catch
        {
            return null;
        }
    }

    private static async Task OpenIfNeededAsync(IMailFolder folder, CancellationToken cancellationToken)
    {
        if (!folder.IsOpen)
        {
            await folder.OpenAsync(FolderAccess.ReadOnly, cancellationToken);
        }
    }

    private static async Task AddFolderAsync(IMailFolder folder, ICollection<MailFolderDto> folders, CancellationToken cancellationToken)
    {
        await OpenIfNeededAsync(folder, cancellationToken);
        var unreadCount = await CountUnreadAsync(folder, cancellationToken);

        folders.Add(new MailFolderDto
        {
            Name = folder.FullName,
            DisplayName = folder.Name,
            IsSelectable = true,
            UnreadCount = unreadCount,
            TotalCount = folder.Count
        });

        if (folder.IsOpen)
        {
            await folder.CloseAsync(false, cancellationToken);
        }
    }

    private static async Task<int> CountUnreadAsync(IMailFolder folder, CancellationToken cancellationToken)
    {
        var unread = await folder.SearchAsync(SearchQuery.NotSeen, cancellationToken);
        return unread.Count;
    }

    private static MailMessageSummaryDto ToSummary(string folderName, UniqueId uid, MimeMessage message, bool isRead)
    {
        return new MailMessageSummaryDto
        {
            FolderName = folderName,
            UniqueId = uid.Id,
            Subject = message.Subject ?? string.Empty,
            From = JoinAddresses(message.From),
            To = JoinAddresses(message.To),
            DateReceived = message.Date,
            IsRead = isRead,
            SizeInBytes = GetMessageSize(message),
            Preview = BuildPreview(message)
        };
    }

    private static MailMessageDetailDto ToDetail(string folderName, UniqueId uid, MimeMessage message, bool isRead)
    {
        return new MailMessageDetailDto
        {
            FolderName = folderName,
            UniqueId = uid.Id,
            Subject = message.Subject ?? string.Empty,
            From = JoinAddresses(message.From),
            To = JoinAddresses(message.To),
            Cc = JoinAddresses(message.Cc),
            DateReceived = message.Date,
            IsRead = isRead,
            TextBody = message.TextBody ?? string.Empty,
            HtmlBody = message.HtmlBody ?? string.Empty
        };
    }

    private static string JoinAddresses(InternetAddressList addresses)
    {
        var joined = string.Join(", ", addresses.Mailboxes.Select(mailbox => mailbox.ToString()));
        return WebUtility.HtmlDecode(joined);
    }

    private static string BuildPreview(MimeMessage message)
    {
        var text = message.TextBody;
        if (!string.IsNullOrWhiteSpace(text))
        {
            return Trim(text);
        }

        if (!string.IsNullOrWhiteSpace(message.HtmlBody))
        {
            var plainText = HtmlTagRegex.Replace(message.HtmlBody, " ");
            return Trim(WebUtility.HtmlDecode(plainText));
        }

        return string.Empty;
    }

    private static string Trim(string value)
    {
        var normalized = string.Join(' ', value.Split(['\r', '\n', '\t'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        return normalized.Length <= 180 ? normalized : normalized[..180];
    }

    private static int GetMessageSize(MimeMessage message)
    {
        using var stream = new MemoryStream();
        message.WriteTo(stream);
        return (int)Math.Min(int.MaxValue, stream.Length);
    }
}