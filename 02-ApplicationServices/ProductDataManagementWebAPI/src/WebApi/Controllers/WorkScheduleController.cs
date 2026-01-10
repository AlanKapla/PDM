using Business.Interfaces.Constants;
using CQRS.WorkSchedules.AnalyzeWorkSchedule;
using CQRS.WorkSchedules.CreateWorkSchedule;
using CQRS.WorkSchedules.GetWorkSchedules;
using CQRS.WorkSchedules.GetWorkSchedule;
using CQRS.WorkSchedules.UpdateWorkSchedule;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CQRS.WorkSchedules.GetUserAssignedWorks;

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
        /// Gets all works assigned to the current user in their active tenant
        /// Grouped by Project > WorkSchedule > Stage > Work with period information
        /// </summary>
        /// <param name="tenantId">The active tenant ID</param>
        /// <returns>Hierarchically grouped assigned works with periods</returns>
        [HttpGet("~/api/tenants/{tenantId}/my-assigned-works")]
        [Authorize(Policy = PermissionCodes.TenantView)]
        public async Task<IActionResult> GetMyAssignedWorks([FromRoute] Guid tenantId)
        {
            var query = new GetUserAssignedWorksQuery(tenantId);
            var result = await Send(query);
            return Ok(result);
        }

        /// <summary>
        /// Analyzes a work schedule using AI to detect conflicts, resource issues, and provide recommendations
        /// AI will automatically check for: time conflicts, resource overallocation, unassigned works, and workload imbalances
        /// Requires ProjectResourcesWrite permission or ownership of the work schedule
        /// </summary>
        /// <param name="tenantId">The tenant ID</param>
        /// <param name="projectId">The project ID</param>
        /// <param name="workScheduleId">The work schedule ID to analyze</param>
        /// <returns>Comprehensive AI analysis with findings, recommendations, and detected conflicts</returns>
        [HttpPost("analyze/{workScheduleId}")]
        [Authorize(Policy = PermissionCodes.ProjectResourcesWrite)]
        public async Task<IActionResult> AnalyzeWorkSchedule(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid workScheduleId)
        {
            var command = new AnalyzeWorkScheduleCommand(tenantId, projectId, workScheduleId);
            var result = await Send(command);
            return Ok(result);
        }
    }
}
