using Business.Interfaces.Constants;
using Business.Interfaces.WebModels.Roles;
using CQRS.Roles.GetAvailableRoles;
using Entities.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    /// <summary>
    /// Controller for managing roles
    /// </summary>
    [Route("api/role")]
    [ApiController]
    public class RoleController(IMediator mediator) : BaseApiController(mediator)
    {
        /// <summary>
        /// Get all available roles for a specific scope (Tenant or Project)
        /// </summary>
        /// <param name="scope">Role scope: 0 = Tenant, 1 = Project</param>
        /// <returns>List of available roles</returns>
        /// <response code="200">Returns the list of roles</response>
        /// <response code="400">Invalid scope value</response>
        /// <response code="403">User does not have permission to list roles</response>
        [HttpGet]
        [Authorize(Policy = PermissionCodes.RoleList)]
        [ProducesResponseType(typeof(IEnumerable<RoleWeb>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAvailableRoles([FromQuery] RoleScope scope)
        {
            var query = new GetAvailableRolesQuery(scope);
            var result = await Send(query);
            return Ok(result);
        }

        /// <summary>
        /// Get all available tenant roles
        /// </summary>
        /// <returns>List of tenant roles</returns>
        /// <response code="200">Returns the list of tenant roles</response>
        /// <response code="403">User does not have permission to list roles</response>
        [HttpGet("tenant")]
        [Authorize(Policy = PermissionCodes.RoleList)]
        [ProducesResponseType(typeof(IEnumerable<RoleWeb>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetTenantRoles()
        {
            var query = new GetAvailableRolesQuery(RoleScope.Tenant);
            var result = await Send(query);
            return Ok(result);
        }

        /// <summary>
        /// Get all available project roles
        /// </summary>
        /// <returns>List of project roles</returns>
        /// <response code="200">Returns the list of project roles</response>
        /// <response code="403">User does not have permission to list roles</response>
        [HttpGet("project")]
        [Authorize(Policy = PermissionCodes.RoleList)]
        [ProducesResponseType(typeof(IEnumerable<RoleWeb>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetProjectRoles()
        {
            var query = new GetAvailableRolesQuery(RoleScope.Project);
            var result = await Send(query);
            return Ok(result);
        }
    }
}
