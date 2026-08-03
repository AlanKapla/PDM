using Business.Interfaces.WebModels.Admin;
using Business.Interfaces.WebModels.Users;
using CQRS.Admin.ActivityLogs.GetUserActivityLogs;
using CQRS.Admin.ColdMails.GetColdMailHistory;
using CQRS.Admin.ColdMails.GetColdMailTemplate;
using CQRS.Admin.ColdMails.SendColdMails;
using CQRS.Admin.Users.GetAdminUsers;
using CQRS.Admin.Users.SendWelcomeEmailToUser;
using CQRS.Admin.WelcomeEmails.SendWelcomeEmailsToExistingUsers;
using Entities.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Authorization;

namespace WebApi.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Policy = AuthorizationPolicyNames.SuperAdminOnly)]
public sealed class AdminController(IMediator mediator) : BaseApiController(mediator)
{
    /// <summary>
    /// Returns all users for SuperAdmin management.
    /// </summary>
    [HttpGet("users")]
    [ProducesResponseType(typeof(IReadOnlyList<AdminUserWeb>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUsers()
    {
        GetAdminUsersQuery query = new();
        IReadOnlyList<AdminUserWeb> result = await Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Sends a welcome email to a single user. SuperAdmin only.
    /// </summary>
    [HttpPost("users/{userId:guid}/welcome-email")]
    [ProducesResponseType(typeof(AdminUserWeb), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SendWelcomeEmailToUser([FromRoute] Guid userId)
    {
        SendWelcomeEmailToUserCommand command = new(userId);
        AdminUserWeb result = await Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Sends welcome emails to all existing users who haven't received one yet. SuperAdmin only.
    /// </summary>
    [HttpPost("welcome-emails/send")]
    [ProducesResponseType(typeof(SendWelcomeEmailsResultWeb), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> SendWelcomeEmailsToExistingUsers()
    {
        SendWelcomeEmailsToExistingUsersCommand command = new();
        SendWelcomeEmailsResultWeb result = await Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Returns the raw cold-mail.html template for client-side live preview. SuperAdmin only.
    /// </summary>
    [HttpGet("cold-mails/template")]
    [ProducesResponseType(typeof(ColdMailTemplateWeb), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetColdMailTemplate()
    {
        ColdMailTemplateWeb result = await Send(new GetColdMailTemplateQuery());
        return Ok(result);
    }

    /// <summary>
    /// Sends cold mails to a list of recipient emails. SuperAdmin only.
    /// </summary>
    [HttpPost("cold-mails/send")]
    [ProducesResponseType(typeof(SendColdMailsResultWeb), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> SendColdMails([FromBody] SendColdMailsCommand command)
    {
        SendColdMailsResultWeb result = await Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Returns cold mail send history, optionally filtered by recipient email. SuperAdmin only.
    /// </summary>
    [HttpGet("cold-mails")]
    [ProducesResponseType(typeof(IReadOnlyList<ColdMailHistoryWeb>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetColdMailHistory([FromQuery] string? email)
    {
        GetColdMailHistoryQuery query = new(email);
        IReadOnlyList<ColdMailHistoryWeb> result = await Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Returns user activity logs (login / demo), optionally filtered by event type. SuperAdmin only.
    /// </summary>
    [HttpGet("activity-logs")]
    [ProducesResponseType(typeof(IReadOnlyList<UserActivityLogWeb>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUserActivityLogs([FromQuery] UserActivityEventType? eventType)
    {
        GetUserActivityLogsQuery query = new(eventType);
        IReadOnlyList<UserActivityLogWeb> result = await Send(query);
        return Ok(result);
    }
}
