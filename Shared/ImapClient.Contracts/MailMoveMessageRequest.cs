namespace ImapClient.Contracts;

public sealed class MailMoveMessageRequest
{
    public MailConnectionRequest Connection { get; set; } = new();

    public string SourceFolderName { get; set; } = "INBOX";

    public string DestinationFolderName { get; set; } = string.Empty;

    public uint UniqueId { get; set; }
}
