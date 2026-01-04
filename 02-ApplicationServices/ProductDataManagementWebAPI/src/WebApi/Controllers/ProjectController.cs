using Business.Interfaces.Constants;
using CQRS.Projects.AddProjectMember;
using CQRS.Projects.CreateProject;
using CQRS.Projects.GetProjectDetails;
using CQRS.Projects.GetProjectMembers;
using CQRS.Projects.GetProjectsDictionary;
using CQRS.Projects.GetTenantProjects;
using CQRS.Projects.RemoveProjectMember;
using CQRS.Projects.ToggleProjectStatus;
using CQRS.Projects.UpdateProject;
using CQRS.Projects.UpdateProjectMemberRole;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [Route("api/tenants/{tenantId}/project")]
    [ApiController]
    public class ProjectController(IMediator mediator) : BaseApiController(mediator)
    {
        [HttpGet]
        [Authorize(Policy = PermissionCodes.TenantView)]
        public async Task<IActionResult> GetTenantProjects([FromRoute] Guid tenantId)
        {
            var query = new GetTenantProjectsQuery(tenantId);
            var result = await Send(query);
            return Ok(result);
        }

        [HttpGet("dictionary")]
        [Authorize(Policy = PermissionCodes.TenantView)]
        public async Task<IActionResult> GetProjectsDictionary([FromRoute] Guid tenantId)
        {
            var query = new GetProjectsDictionaryQuery(tenantId);
            var result = await Send(query);
            return Ok(result);
        }

        [HttpPost]
        [Authorize(Policy = PermissionCodes.TenantProjectCreate)]
        public async Task<IActionResult> CreateProject([FromRoute] Guid tenantId, [FromBody] CreateProjectCommand command)
        {
            var result = await Send(command);
            return CreatedAtAction(nameof(GetTenantProjects), new { tenantId }, result);
        }

        [HttpGet("{projectId}")]
        [Authorize(Policy = PermissionCodes.ProjectView)]
        public async Task<IActionResult> GetProjectDetails(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId)
        {
            var query = new GetProjectDetailsQuery(tenantId, projectId);
            var result = await Send(query);
            return Ok(result);
        }

        [HttpPut("{projectId}")]
        [Authorize(Policy = PermissionCodes.ProjectEdit)]
        public async Task<IActionResult> UpdateProject(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromBody] UpdateProjectCommand command)
        {
            command = command with { TenantId = tenantId, ProjectId = projectId };
            var result = await Send(command);
            return Ok(result);
        }

        [HttpGet("{projectId}/members")]
        [Authorize(Policy = PermissionCodes.ProjectMembersView)]
        public async Task<IActionResult> GetProjectMembers(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId)
        {
            var query = new GetProjectMembersQuery(tenantId, projectId);
            var result = await Send(query);
            return Ok(result);
        }

        [HttpPost("{projectId}/members")]
        [Authorize(Policy = PermissionCodes.ProjectMembersManage)]
        public async Task<IActionResult> AddProjectMember(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromBody] AddProjectMemberCommand command)
        {
            command = command with { TenantId = tenantId, ProjectId = projectId };  
            await Send(command);
            return NoContent();
        }

        [HttpDelete("{projectId}/members/{userId}")]
        [Authorize(Policy = PermissionCodes.ProjectMembersManage)]
        public async Task<IActionResult> RemoveProjectMember(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid userId)
        {
            var command = new RemoveProjectMemberCommand(tenantId, projectId, userId);
            await Send(command);
            return NoContent();
        }

        [HttpPatch("{projectId}/status")]
        [Authorize(Policy = PermissionCodes.ProjectStatusManage)]
        public async Task<IActionResult> ToggleProjectStatus(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromQuery] bool isActive)
        {
            var command = new ToggleProjectStatusCommand(tenantId, projectId, isActive);
            await Send(command);
            return NoContent();
        }

        [HttpPatch("{projectId}/members/{userId}/role")]
        [Authorize(Policy = PermissionCodes.ProjectMembersManage)]
        public async Task<IActionResult> UpdateProjectMemberRole(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid userId,
            [FromBody] UpdateProjectMemberRoleCommand request)
        {
            request = request with { TenantId = tenantId, ProjectId = projectId, UserId = userId };
            await Send(request);
            return NoContent();
        }
    }
}
