# ImapClient

Blazor-based mail dashboard for Outlook 365 / IMAP.

## Projects

- `Backend/ImapClient.Api`: ASP.NET Core Web API with MailKit IMAP operations.
- `Frontend/ImapClient.Web`: Blazor Web App UI for browsing and managing mail.
- `Shared/ImapClient.Contracts`: DTOs shared by both projects.

## What it does

- Lists common IMAP folders.
- Loads recent messages from a selected folder.
- Opens message details.
- Marks messages read or unread.
- Deletes messages or moves them to another folder.

## Notes

- Microsoft 365 IMAP commonly requires OAuth2. The UI supports both OAuth2 access tokens and password-based IMAP if the tenant allows it.
- The front end includes an API base URL field so you can point it at the backend when the local port differs from the default.

## Solution

Open `ImapClient.slnx` in VS Code or Visual Studio to work with all three projects together.
