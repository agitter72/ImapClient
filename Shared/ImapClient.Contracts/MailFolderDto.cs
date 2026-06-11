namespace ImapClient.Contracts;

public sealed record MailFolderDto
{
    public string Name { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public bool IsSelectable { get; init; }

    public int UnreadCount { get; init; }

    public int TotalCount { get; init; }
}
