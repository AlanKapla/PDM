using Business.Interfaces.Constants;
using Business.Interfaces.WebModels.Projects;
using CQRS.Projects.AddProjectMember;
using CQRS.Projects.AddProjectUnit;
using CQRS.Projects.AddProjectCostCategory;
using CQRS.Projects.CreateProject;
using CQRS.Projects.DeleteProjectUnit;
using CQRS.Projects.DeleteProjectCostCategory;
using CQRS.Projects.GetProjectDetails;
using CQRS.Projects.GetProjectInvitations;
using CQRS.Projects.GetProjectMembers;
using CQRS.Projects.GetProjectsDictionary;
using CQRS.Projects.GetProjectUnits;
using CQRS.Projects.GetProjectCostCategories;
using CQRS.Projects.GetTenantProjects;
using CQRS.Projects.InviteProjectMember;
using CQRS.Projects.RemoveProjectInvitation;
using CQRS.Projects.RemoveProjectMember;
using CQRS.Projects.ReorderProjectUnits;
using CQRS.Projects.ReorderProjectCostCategories;
using CQRS.Projects.SetProjectCurrency;
using CQRS.Projects.ToggleProjectStatus;
using CQRS.Projects.UpdateProject;
using CQRS.Projects.UpdateProjectMemberRole;
using CQRS.Projects.UpdateProjectUnit;
using CQRS.Projects.UpdateProjectCostCategory;
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
            [FromBody] UpdateProjectWeb body)
        {
            UpdateProjectCommand command = new UpdateProjectCommand
            {
                TenantId = tenantId,
                ProjectId = projectId,
                Name = body.Name
            };
            ProjectDetailsWeb result = await Send(command);
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

        [HttpPost("{projectId}/invitations")]
        [Authorize(Policy = PermissionCodes.ProjectMembers)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> InviteProjectMember(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromBody] InviteProjectMemberCommand command)
        {
            command = command with { TenantId = tenantId, ProjectId = projectId };
            await Send(command);
            return NoContent();
        }

        [HttpGet("{projectId}/invitations")]
        [Authorize(Policy = PermissionCodes.ProjectMembers)]
        public async Task<IActionResult> GetProjectInvitations(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId)
        {
            GetProjectInvitationsQuery query = new GetProjectInvitationsQuery
            {
                TenantId = tenantId,
                ProjectId = projectId
            };
            IEnumerable<ProjectInvitationWeb> result = await Send(query);
            return Ok(result);
        }

        [HttpDelete("{projectId}/invitations/{invitationId}")]
        [Authorize(Policy = PermissionCodes.ProjectMembers)]
        public async Task<IActionResult> RemoveProjectInvitation(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid invitationId)
        {
            RemoveProjectInvitationCommand command = new RemoveProjectInvitationCommand
            {
                TenantId = tenantId,
                ProjectId = projectId,
                InvitationId = invitationId
            };
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

        [HttpGet("{projectId}/units")]
        [Authorize(Policy = PermissionCodes.ProjectView)]
        [ProducesResponseType(typeof(List<ProjectUnitWeb>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetProjectUnits(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId)
        {
            GetProjectUnitsQuery query = new GetProjectUnitsQuery { TenantId = tenantId, ProjectId = projectId };
            List<ProjectUnitWeb> result = await Send(query);
            return Ok(result);
        }

        [HttpPost("{projectId}/units")]
        [Authorize(Policy = PermissionCodes.ProjectSettings)]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AddProjectUnit(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromBody] UpsertProjectUnitWeb body)
        {
            AddProjectUnitCommand command = new AddProjectUnitCommand
            {
                TenantId = tenantId,
                ProjectId = projectId,
                Code = body.Code,
                Name = body.Name,
                Symbol = body.Symbol
            };
            Guid newId = await Send(command);
            return CreatedAtAction(nameof(GetProjectUnits), new { tenantId, projectId }, newId);
        }

        [HttpPut("{projectId}/units/{unitId:guid}")]
        [Authorize(Policy = PermissionCodes.ProjectSettings)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateProjectUnit(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid unitId,
            [FromBody] UpsertProjectUnitWeb body)
        {
            UpdateProjectUnitCommand command = new UpdateProjectUnitCommand
            {
                TenantId = tenantId,
                ProjectId = projectId,
                UnitId = unitId,
                Code = body.Code,
                Name = body.Name,
                Symbol = body.Symbol,
                Order = body.Order
            };
            await Send(command);
            return NoContent();
        }

        [HttpDelete("{projectId}/units/{unitId:guid}")]
        [Authorize(Policy = PermissionCodes.ProjectSettings)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteProjectUnit(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid unitId)
        {
            DeleteProjectUnitCommand command = new DeleteProjectUnitCommand
            {
                TenantId = tenantId,
                ProjectId = projectId,
                UnitId = unitId
            };
            await Send(command);
            return NoContent();
        }

        [HttpPost("{projectId}/units/reorder")]
        [Authorize(Policy = PermissionCodes.ProjectSettings)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ReorderProjectUnits(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromBody] List<Guid> unitIds)
        {
            ReorderProjectUnitsCommand command = new ReorderProjectUnitsCommand
            {
                TenantId = tenantId,
                ProjectId = projectId,
                UnitIds = unitIds
            };
            await Send(command);
            return NoContent();
        }

        [HttpGet("{projectId}/cost-categories")]
        [Authorize(Policy = PermissionCodes.ProjectView)]
        [ProducesResponseType(typeof(List<ProjectCostCategoryWeb>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetProjectCostCategories(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId)
        {
            GetProjectCostCategoriesQuery query = new GetProjectCostCategoriesQuery
            {
                TenantId = tenantId,
                ProjectId = projectId
            };
            List<ProjectCostCategoryWeb> result = await Send(query);
            return Ok(result);
        }

        [HttpPost("{projectId}/cost-categories")]
        [Authorize(Policy = PermissionCodes.ProjectSettings)]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AddProjectCostCategory(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromBody] UpsertProjectCostCategoryWeb body)
        {
            AddProjectCostCategoryCommand command = new AddProjectCostCategoryCommand
            {
                TenantId = tenantId,
                ProjectId = projectId,
                Name = body.Name,
                Code = body.Code,
                Color = body.Color
            };
            Guid newId = await Send(command);
            return CreatedAtAction(nameof(GetProjectCostCategories), new { tenantId, projectId }, newId);
        }

        [HttpPut("{projectId}/cost-categories/{categoryId:guid}")]
        [Authorize(Policy = PermissionCodes.ProjectSettings)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateProjectCostCategory(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid categoryId,
            [FromBody] UpsertProjectCostCategoryWeb body)
        {
            UpdateProjectCostCategoryCommand command = new UpdateProjectCostCategoryCommand
            {
                TenantId = tenantId,
                ProjectId = projectId,
                CategoryId = categoryId,
                Name = body.Name,
                Code = body.Code,
                Order = body.Order,
                Color = body.Color
            };
            await Send(command);
            return NoContent();
        }

        [HttpDelete("{projectId}/cost-categories/{categoryId:guid}")]
        [Authorize(Policy = PermissionCodes.ProjectSettings)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteProjectCostCategory(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid categoryId)
        {
            DeleteProjectCostCategoryCommand command = new DeleteProjectCostCategoryCommand
            {
                TenantId = tenantId,
                ProjectId = projectId,
                CategoryId = categoryId
            };
            await Send(command);
            return NoContent();
        }

        [HttpPost("{projectId}/cost-categories/reorder")]
        [Authorize(Policy = PermissionCodes.ProjectSettings)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ReorderProjectCostCategories(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromBody] List<Guid> categoryIds)
        {
            ReorderProjectCostCategoriesCommand command = new ReorderProjectCostCategoriesCommand
            {
                TenantId = tenantId,
                ProjectId = projectId,
                CategoryIds = categoryIds
            };
            await Send(command);
            return NoContent();
        }
    }
}
