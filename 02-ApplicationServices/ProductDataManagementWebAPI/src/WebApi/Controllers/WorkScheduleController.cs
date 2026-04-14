using Business.Interfaces.Constants;
using CQRS.WorkSchedules.CreateWorkSchedule;
using CQRS.WorkSchedules.DeleteWorkSchedule;
using CQRS.WorkSchedules.GetWorkSchedules;
using CQRS.WorkSchedules.GetWorkSchedule;
using CQRS.WorkSchedules.SyncWorkScheduleWithEstimate;
using CQRS.WorkSchedules.UpdateWorkSchedule;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    /// <summary>
    /// Controller for managing work schedules within projects
    /// </summary>
    [Route("api/tenants/{tenantId}/project/{projectId}/work-schedule")]
    [ApiController]
    public class WorkScheduleController(IMediator mediator) : BaseApiController(mediator)
    {
        /// <summary>
        /// Creates a new work schedule for a project with stages and work assignments
        /// </summary>
        /// <param name="tenantId">The tenant ID</param>
        /// <param name="projectId">The project ID</param>
        /// <param name="command">The work schedule creation details</param>
        /// <returns>The created work schedule with all stages and works</returns>
        [HttpPost]
        [Authorize(Policy = PermissionCodes.ProjectResourcesWrite)]
        public async Task<IActionResult> CreateWorkSchedule(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromBody] CreateWorkScheduleCommand command)
        {
            command = command with { TenantId = tenantId, ProjectId = projectId };
            var result = await Send(command);
            return CreatedAtAction(nameof(CreateWorkSchedule), new { tenantId, projectId }, result);
        }

        /// <summary>
        /// Updates an existing work schedule with stages and work assignments
        /// </summary>
        /// <param name="tenantId">The tenant ID</param>
        /// <param name="projectId">The project ID</param>
        /// <param name="workScheduleId">The work schedule ID</param>
        /// <param name="command">The work schedule update details</param>
        /// <returns>The updated work schedule with all stages and works</returns>
        [HttpPut("{workScheduleId}")]
        [Authorize(Policy = PermissionCodes.ProjectResourcesWrite)]
        public async Task<IActionResult> UpdateWorkSchedule(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid workScheduleId,
            [FromBody] UpdateWorkScheduleCommand command)
        {
            command = command with { TenantId = tenantId, ProjectId = projectId, WorkScheduleId = workScheduleId };
            var result = await Send(command);
            return Ok(result);
        }

        /// <summary>
        /// Get work schedules based on scope (All, Mine, Shared)
        /// </summary>
        /// <param name="tenantId">Tenant ID</param>
        /// <param name="projectId">Project ID</param>
        /// <param name="scope">Resource scope (All, Mine, Shared)</param>
        /// <returns>List of work schedules</returns>
        [HttpGet("{scope}")]
        [Authorize(Policy = PermissionCodes.ProjectView)]
        public async Task<IActionResult> GetWorkSchedules(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] ResourceScope scope)
        {
            var query = new GetWorkSchedulesQuery(tenantId, projectId, scope);
            var result = await Send(query);
            return Ok(result);
        }

        /// <summary>
        /// Gets a work schedule by ID with full details including stages, works and assignments
        /// </summary>
        /// <param name="tenantId">The tenant ID</param>
        /// <param name="projectId">The project ID</param>
        /// <param name="workScheduleId">The work schedule ID</param>
        /// <returns>The work schedule with all details</returns>
        [HttpGet("details/{workScheduleId}")]
        [Authorize(Policy = PermissionCodes.ProjectResourcesReadSingle)]
        public async Task<IActionResult> GetWorkSchedule(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid workScheduleId)
        {
            var query = new GetWorkScheduleQuery(tenantId, projectId, workScheduleId);
            var result = await Send(query);
            return Ok(result);
        }

        /// <summary>
        /// Synchronizes a work schedule with its linked cost estimate.
        /// Requires the work schedule to be linked to a cost estimate and full access to it.
        /// </summary>
        /// <param name="tenantId">The tenant ID</param>
        /// <param name="projectId">The project ID</param>
        /// <param name="workScheduleId">The work schedule ID</param>
        [HttpPost("{workScheduleId}/sync-with-estimate")]
        [Authorize(Policy = PermissionCodes.ProjectResourcesWrite)]
        public async Task<IActionResult> SyncWorkScheduleWithEstimate(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid workScheduleId)
        {
            var command = new SyncWorkScheduleWithEstimateCommand(workScheduleId) with { TenantId = tenantId, ProjectId = projectId };
            await Send(command);
            return NoContent();
        }

        /// <summary>
        /// Deletes a work schedule. Only the owner or a tenant/project admin can delete it.
        /// </summary>
        /// <param name="tenantId">The tenant ID</param>
        /// <param name="projectId">The project ID</param>
        /// <param name="workScheduleId">The work schedule ID</param>
        [HttpDelete("{workScheduleId}")]
        [Authorize(Policy = PermissionCodes.ProjectResourcesWrite)]
        public async Task<IActionResult> DeleteWorkSchedule(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid workScheduleId)
        {
            var command = new DeleteWorkScheduleCommand(workScheduleId) with { TenantId = tenantId, ProjectId = projectId };
            await Send(command);
            return NoContent();
        }
    }
}
