using Business.Interfaces.WebModels.Projects;
using CQRS.Projects.AcceptProjectInvitation;
using CQRS.Projects.ActiveProjectInvitations;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[Route("api/projects")]
[ApiController]
public sealed class ProjectInvitationsController(IMediator mediator) : BaseApiController(mediator)
{
    [HttpGet("invitations")]
    [Authorize]
    public async Task<IActionResult> GetActiveInvitations()
    {
        IEnumerable<ProjectInvitationWeb> result = await Send(new ActiveProjectInvitationsQuery());
        return Ok(result);
    }

    [HttpPost("invitations/accept")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> AcceptInvitation([FromBody] AcceptProjectInvitationCommand request)
    {
        await Send(request);
        return NoContent();
    }
}
