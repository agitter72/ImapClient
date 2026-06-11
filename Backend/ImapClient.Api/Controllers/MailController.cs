using ImapClient.Api.Services;
using ImapClient.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace ImapClient.Api.Controllers;

[ApiController]
[Route("api/mail")]
public sealed class MailController(IImapMailService mailService) : ControllerBase
{
    [HttpPost("folders")]
    public async Task<ActionResult<IReadOnlyList<MailFolderDto>>> GetFolders([FromBody] MailConnectionRequest connection, CancellationToken cancellationToken)
    {
        var folders = await mailService.GetFoldersAsync(connection, cancellationToken);
        return Ok(folders);
    }

    [HttpPost("messages")]
    public async Task<ActionResult<IReadOnlyList<MailMessageSummaryDto>>> GetMessages([FromBody] MailFolderQueryRequest request, CancellationToken cancellationToken)
    {
        var messages = await mailService.GetMessagesAsync(request, cancellationToken);
        return Ok(messages);
    }

    [HttpPost("message")]
    public async Task<ActionResult<MailMessageDetailDto>> GetMessage([FromBody] MailMessageLookupRequest request, CancellationToken cancellationToken)
    {
        var message = await mailService.GetMessageAsync(request, cancellationToken);
        return message is null ? NotFound() : Ok(message);
    }

    [HttpPost("message/read")]
    public async Task<IActionResult> MarkAsRead([FromBody] MailMessageLookupRequest request, CancellationToken cancellationToken)
    {
        await mailService.MarkAsReadAsync(request, cancellationToken);
        return NoContent();
    }

    [HttpPost("message/unread")]
    public async Task<IActionResult> MarkAsUnread([FromBody] MailMessageLookupRequest request, CancellationToken cancellationToken)
    {
        await mailService.MarkAsUnreadAsync(request, cancellationToken);
        return NoContent();
    }

    [HttpDelete("message")]
    public async Task<IActionResult> DeleteMessage([FromBody] MailMessageLookupRequest request, CancellationToken cancellationToken)
    {
        await mailService.DeleteAsync(request, cancellationToken);
        return NoContent();
    }

    [HttpPost("message/move")]
    public async Task<IActionResult> MoveMessage([FromBody] MailMoveMessageRequest request, CancellationToken cancellationToken)
    {
        await mailService.MoveAsync(request, cancellationToken);
        return NoContent();
    }
}