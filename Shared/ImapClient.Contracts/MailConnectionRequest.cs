namespace ImapClient.Contracts;

public sealed class MailConnectionRequest
{
    public string Host { get; set; } = "imap.1und1.de";

    public int Port { get; set; } = 993;

    public bool UseSsl { get; set; } = true;

    public bool UseOAuth2 { get; set; } = false;

    public string Username { get; set; } = "info@alex-gitter.de";

    public string? Password { get; set; }

    public string? AccessToken { get; set; }
}
