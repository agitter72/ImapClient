namespace ImapClient.Contracts;

public sealed class MailFolderQueryRequest
{
    public MailConnectionRequest Connection { get; set; } = new();

    public string FolderName { get; set; } = "INBOX";

    public int Take { get; set; } = 25;
}
