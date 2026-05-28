using Business.Interfaces.Constants;
using Business.Interfaces.WebModels.Roles;
using CQRS.Roles.GetAvailableRoles;
using Entities.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [Route("api/role")]
    [ApiController]
    public class RoleController(IMediator mediator) : BaseApiController(mediator)
    {
        [HttpGet]
        [Authorize(Policy = PermissionCodes.RoleList)]
        [ProducesResponseType(typeof(IEnumerable<RoleWeb>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAvailableRoles([FromQuery] RoleScope scope)
        {
            GetAvailableRolesQuery query = new GetAvailableRolesQuery(scope);
            IEnumerable<RoleWeb> result = await Send(query);
            return Ok(result);
        }

        [HttpGet("tenant")]
        [Authorize(Policy = PermissionCodes.RoleList)]
        [ProducesResponseType(typeof(IEnumerable<RoleWeb>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetTenantRoles()
        {
            GetAvailableRolesQuery query = new GetAvailableRolesQuery(RoleScope.Tenant);
            IEnumerable<RoleWeb> result = await Send(query);
            return Ok(result);
        }
    }
}
