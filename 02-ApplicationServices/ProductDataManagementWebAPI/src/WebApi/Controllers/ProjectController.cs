using CQRS.Projects.AddProjectMember;
using CQRS.Projects.CreateProject;
using CQRS.Projects.GetProjectDetails;
using CQRS.Projects.GetProjectMembers;
using CQRS.Projects.GetTenantProjects;
using CQRS.Projects.RemoveProjectMember;
using CQRS.Projects.ToggleProjectStatus;
using CQRS.Projects.UpdateProject;
using CQRS.Projects.UpdateProjectMemberRole;
using Entities.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Constants;
using Business.Interfaces.Helpers;

namespace WebApi.Controllers
{
    [Route("api/tenants/{tenantId}/[controller]")]
    [ApiController]
    public class ProjectController(IMediator mediator) : BaseApiController(mediator)
    {
        [HttpGet]
        [Authorize(Policy = Policies.TenantMember)]
        public async Task<IActionResult> GetTenantProjects([FromRoute] Guid tenantId)
        {
            var query = new GetTenantProjectsQuery(tenantId);
            var result = await Send(query);
            return Ok(result);
        }

        [HttpPost]
        [Authorize(Policy = Policies.TenantAdmin)]
        public async Task<IActionResult> CreateProject([FromRoute] Guid tenantId, [FromBody] CreateProjectCommand command)
        {
            var result = await Send(command);
            return CreatedAtAction(nameof(GetTenantProjects), new { tenantId }, result);
        }

        [HttpGet("{projectId}")]
        [Authorize(Policy = Policies.ProjectMember)]
        public async Task<IActionResult> GetProjectDetails(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId)
        {
            var query = new GetProjectDetailsQuery(tenantId, projectId);
            var result = await Send(query);
            
            return Ok(result);
        }

        /// <summary>
        /// Update project details
        /// </summary>
        /// <param name="tenantId">Tenant identifier</param>
        /// <param name="projectId">Project identifier</param>
        /// <param name="command">Update project command</param>
        /// <returns>Updated project details</returns>
        [HttpPut("{projectId}")]
        [Authorize(Policy = Policies.ProjectAdmin)]
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
        [Authorize(Policy = Policies.ProjectMember)]
        public async Task<IActionResult> GetProjectMembers(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId)
        {
            var query = new GetProjectMembersQuery(tenantId, projectId);
            var result = await Send(query);
            return Ok(result);
        }

        [HttpPost("{projectId}/members")]
        [Authorize(Policy = Policies.ProjectAdmin)]
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
        [Authorize(Policy = Policies.ProjectAdmin)]
        public async Task<IActionResult> RemoveProjectMember(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid userId)
        {
            var command = new RemoveProjectMemberCommand(tenantId, projectId, userId);
            await Send(command);
            return NoContent();
        }

        [HttpPatch("{projectId}/toggle-status")]
        [Authorize(Policy = Policies.ProjectAdmin)]
        public async Task<IActionResult> ToggleProjectStatus(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromQuery] bool isActive)
        {
            var command = new ToggleProjectStatusCommand(tenantId, projectId, isActive);
            await Send(command);
            return NoContent();
        }

        /// <summary>
        /// Get available project roles dictionary
        /// </summary>
        /// <returns>Dictionary of project roles</returns>
        [HttpGet("roles")]
        [Authorize(Policy = Policies.TenantMember)]
        public IActionResult GetProjectRoles()
        {
            var roles = Enum.GetValues<ProjectRole>()
                .Select(r => new { Value = (int)r, Name = r.GetDisplayName() })
                .ToList();

            return Ok(roles);
        }

        /// <summary>
        /// Update project member role
        /// </summary>
        /// <param name="tenantId">Tenant identifier</param>
        /// <param name="projectId">Project identifier</param>
        /// <param name="userId">User identifier</param>
        /// <param name="request">New role information</param>
        /// <returns>No content</returns>
        [HttpPatch("{projectId}/members/{userId}/role")]
        [Authorize(Policy = Policies.ProjectAdmin)]
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
