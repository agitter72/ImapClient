namespace ImapClient.Contracts;

public sealed class MailMessageLookupRequest
{
    public MailConnectionRequest Connection { get; set; } = new();

    public string FolderName { get; set; } = "INBOX";

    public uint UniqueId { get; set; }
}
