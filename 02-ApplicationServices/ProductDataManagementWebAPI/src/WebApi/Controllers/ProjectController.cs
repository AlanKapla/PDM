using Business.Interfaces.Constants;
using Business.Interfaces.WebModels.Projects;
using CQRS.Projects.AddProjectMember;
using CQRS.Projects.CreateProject;
using CQRS.Projects.SetProjectCurrency;
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
    [Route("api/tenants/{tenantId}/projects")]
    [ApiController]
    public class ProjectController(IMediator mediator) : BaseApiController(mediator)
    {
        [HttpGet]
        [Authorize(Policy = PermissionCodes.TenantView)]
        public async Task<IActionResult> GetTenantProjects([FromRoute] Guid tenantId)
        {
            GetTenantProjectsQuery query = new GetTenantProjectsQuery { TenantId = tenantId };
            IEnumerable<ProjectDetailsWeb> result = await Send(query);
            return Ok(result);
        }

        [HttpGet("dictionary")]
        [Authorize(Policy = PermissionCodes.TenantView)]
        public async Task<IActionResult> GetProjectsDictionary([FromRoute] Guid tenantId)
        {
            GetProjectsDictionaryQuery query = new GetProjectsDictionaryQuery { TenantId = tenantId };
            Dictionary<Guid, string> result = await Send(query);
            return Ok(result);
        }

        [HttpPost]
        [Authorize(Policy = PermissionCodes.TenantProjectsCreate)]
        public async Task<IActionResult> CreateProject([FromRoute] Guid tenantId, [FromBody] CreateProjectCommand command)
        {
            command = command with { TenantId = tenantId };

            var result = await Send(command);
            return CreatedAtAction(nameof(GetProjectDetails), new { tenantId, projectId = result.Id }, result);
        }

        [HttpGet("{projectId}")]
        [Authorize(Policy = PermissionCodes.ProjectView)]
        public async Task<IActionResult> GetProjectDetails(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId)
        {
            GetProjectDetailsQuery query = new GetProjectDetailsQuery { TenantId = tenantId, ProjectId = projectId };
            ProjectDetailsWeb result = await Send(query);
            return Ok(result);
        }

        [HttpPut("{projectId}")]
        [Authorize(Policy = PermissionCodes.ProjectSettings)]
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
        [Authorize(Policy = PermissionCodes.ProjectView)]
        public async Task<IActionResult> GetProjectMembers(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId)
        {
            GetProjectMembersQuery query = new GetProjectMembersQuery { TenantId = tenantId, ProjectId = projectId };
            IEnumerable<ProjectMemberWeb> result = await Send(query);
            return Ok(result);
        }

        [HttpPost("{projectId}/members")]
        [Authorize(Policy = PermissionCodes.ProjectMembers)]
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
        [Authorize(Policy = PermissionCodes.ProjectMembers)]
        public async Task<IActionResult> RemoveProjectMember(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid userId)
        {
            RemoveProjectMemberCommand command = new RemoveProjectMemberCommand
            {
                TenantId = tenantId,
                ProjectId = projectId,
                UserId = userId
            };
            await Send(command);
            return NoContent();
        }

        [HttpPatch("{projectId}/status")]
        [Authorize(Policy = PermissionCodes.ProjectSettings)]
        public async Task<IActionResult> ToggleProjectStatus(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromQuery] bool isActive)
        {
            ToggleProjectStatusCommand command = new ToggleProjectStatusCommand
            {
                TenantId = tenantId,
                ProjectId = projectId,
                IsActive = isActive
            };
            await Send(command);
            return NoContent();
        }

        [HttpPut("{projectId}/currency")]
        [Authorize(Policy = PermissionCodes.ProjectSettings)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> SetProjectCurrency(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromBody] SetProjectCurrencyRequest request)
        {
            SetProjectCurrencyCommand command = new SetProjectCurrencyCommand
            {
                TenantId = tenantId,
                ProjectId = projectId,
                Code = request.Code,
                Name = request.Name,
                Symbol = request.Symbol
            };
            await Send(command);
            return NoContent();
        }

        [HttpPatch("{projectId}/members/{userId}/role")]
        [Authorize(Policy = PermissionCodes.ProjectMembers)]
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
