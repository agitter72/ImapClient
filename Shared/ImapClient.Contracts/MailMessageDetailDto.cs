namespace ImapClient.Contracts;

public sealed record MailMessageDetailDto
{
    public string FolderName { get; init; } = string.Empty;

    public uint UniqueId { get; init; }

    public string Subject { get; init; } = string.Empty;

    public string From { get; init; } = string.Empty;

    public string To { get; init; } = string.Empty;

    public string Cc { get; init; } = string.Empty;

    public DateTimeOffset? DateReceived { get; init; }

    public bool IsRead { get; init; }

    public string TextBody { get; init; } = string.Empty;

    public string HtmlBody { get; init; } = string.Empty;
}
