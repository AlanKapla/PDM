using Business.Interfaces.Constants;
using Business.Interfaces.WebModels.Admin;
using CQRS.Admin.Users.GetAdminUserDetails;
using CQRS.Admin.Users.GetAdminUsers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[Route("api/admin/users")]
[ApiController]
[Authorize(Policy = PolicyNames.Admin)]
public sealed class AdminUsersController(IMediator mediator) : BaseApiController(mediator)
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<AdminUserListItemWeb>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUsers()
    {
        GetAdminUsersQuery query = new();
        IEnumerable<AdminUserListItemWeb> result = await Send(query);
        return Ok(result);
    }

    [HttpGet("{userId:guid}")]
    [ProducesResponseType(typeof(AdminUserDetailsWeb), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserDetails([FromRoute] Guid userId)
    {
        GetAdminUserDetailsQuery query = new(userId);
        AdminUserDetailsWeb result = await Send(query);
        return Ok(result);
    }
}
