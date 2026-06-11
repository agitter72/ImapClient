namespace ImapClient.Contracts;

public sealed record MailMessageSummaryDto
{
    public string FolderName { get; init; } = string.Empty;

    public uint UniqueId { get; init; }

    public string Subject { get; init; } = string.Empty;

    public string From { get; init; } = string.Empty;

    public string To { get; init; } = string.Empty;

    public DateTimeOffset? DateReceived { get; init; }

    public bool IsRead { get; init; }

    public int SizeInBytes { get; init; }

    public string Preview { get; init; } = string.Empty;
}
