using System.Net.Http.Json;
using ImapClient.Contracts;
using Microsoft.Extensions.Options;

namespace ImapClient.Web.Services;

public sealed class MailApiClient(HttpClient httpClient, IOptionsMonitor<MailApiOptions> options)
{
    public string? ApiBaseUrl { get; set; }

    public async Task<IReadOnlyList<MailFolderDto>> GetFoldersAsync(MailConnectionRequest connection, CancellationToken cancellationToken = default)
    {
        return await PostAsync<IReadOnlyList<MailFolderDto>>("api/mail/folders", connection, cancellationToken) ?? Array.Empty<MailFolderDto>();
    }

    public async Task<IReadOnlyList<MailMessageSummaryDto>> GetMessagesAsync(MailFolderQueryRequest request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<IReadOnlyList<MailMessageSummaryDto>>("api/mail/messages", request, cancellationToken) ?? Array.Empty<MailMessageSummaryDto>();
    }

    public Task<MailMessageDetailDto?> GetMessageAsync(MailMessageLookupRequest request, CancellationToken cancellationToken = default)
    {
        return PostAsync<MailMessageDetailDto>("api/mail/message", request, cancellationToken);
    }

    public Task MarkAsReadAsync(MailMessageLookupRequest request, CancellationToken cancellationToken = default)
    {
        return SendAsync("api/mail/message/read", request, cancellationToken);
    }

    public Task MarkAsUnreadAsync(MailMessageLookupRequest request, CancellationToken cancellationToken = default)
    {
        return SendAsync("api/mail/message/unread", request, cancellationToken);
    }

    public Task DeleteAsync(MailMessageLookupRequest request, CancellationToken cancellationToken = default)
    {
        return SendAsync("api/mail/message", request, cancellationToken, HttpMethod.Delete);
    }

    public Task MoveAsync(MailMoveMessageRequest request, CancellationToken cancellationToken = default)
    {
        return SendAsync("api/mail/message/move", request, cancellationToken);
    }

    private async Task<T?> PostAsync<T>(string path, object payload, CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsJsonAsync(BuildUri(path), payload, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken);
    }

    private async Task SendAsync(string path, object payload, CancellationToken cancellationToken, HttpMethod? method = null)
    {
        using var request = new HttpRequestMessage(method ?? HttpMethod.Post, BuildUri(path))
        {
            Content = JsonContent.Create(payload)
        };

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private Uri BuildUri(string path)
    {
        var baseUrl = ApiBaseUrl ?? options.CurrentValue.BaseUrl;
        return new Uri(new Uri(baseUrl, UriKind.Absolute), path);
    }
}