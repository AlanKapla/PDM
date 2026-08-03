using CQRS.Activity.RecordDemoActivity;
using CQRS.Activity.RecordLoginActivity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace WebApi.Controllers;

public sealed record RecordActivityRequest(string? Route);

[ApiController]
[Route("api/activity")]
public sealed class ActivityController(IMediator mediator) : BaseApiController(mediator)
{
    /// <summary>
    /// Records a login activity event for the authenticated user. IP is taken from the server.
    /// </summary>
    [HttpPost("login")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RecordLogin(
        [FromBody] RecordActivityRequest? request)
    {
        RecordLoginActivityCommand command = new()
        {
            IpAddress = ResolveClientIp(HttpContext),
            Route = request?.Route
        };

        await Send(command);
        return NoContent();
    }

    /// <summary>
    /// Records a demo-enter activity event. AllowAnonymous — demo session has no JWT.
    /// </summary>
    [HttpPost("demo")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RecordDemo(
        [FromBody] RecordActivityRequest? request)
    {
        RecordDemoActivityCommand command = new()
        {
            IpAddress = ResolveClientIp(HttpContext),
            Route = request?.Route
        };

        await Send(command);
        return NoContent();
    }

    private static string ResolveClientIp(HttpContext httpContext)
    {
        IPAddress? remoteIp = httpContext.Connection.RemoteIpAddress;
        if (remoteIp is null)
        {
            return "unknown";
        }

        if (remoteIp.IsIPv4MappedToIPv6)
        {
            remoteIp = remoteIp.MapToIPv4();
        }

        return remoteIp.ToString();
    }
}
