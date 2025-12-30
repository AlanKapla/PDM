using CQRS.WorkSchedules.CreateWorkSchedule;
using CQRS.WorkSchedules.GetUserWorkSchedules;
using CQRS.WorkSchedules.GetWorkSchedule;
using CQRS.WorkSchedules.UpdateWorkSchedule;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Constants;
using CQRS.WorkSchedules.GetUserAssignedWorks;

namespace WebApi.Controllers
{
    /// <summary>
    /// Controller for managing work schedules within projects
    /// </summary>
    [Route("api/tenants/{tenantId}/projects/{projectId}/work-schedules")]
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
        [Authorize(Policy = Policies.ProjectEditor)]
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
        [Authorize(Policy = Policies.ProjectEditor)]
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
        /// Gets work schedules created by the current user in the project
        /// </summary>
        /// <param name="tenantId">The tenant ID</param>
        /// <param name="projectId">The project ID</param>
        /// <returns>List of work schedules created by the current user</returns>
        [HttpGet("my")]
        [Authorize(Policy = Policies.ProjectEditor)]
        public async Task<IActionResult> GetMyWorkSchedules(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId)
        {
            var query = new GetUserWorkSchedulesQuery(tenantId, projectId);
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
        [HttpGet("{workScheduleId}")]
        [Authorize(Policy = Policies.ProjectEditor)]
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
        [Authorize(Policy = Policies.TenantMember)]
        public async Task<IActionResult> GetMyAssignedWorks([FromRoute] Guid tenantId)
        {
            var query = new GetUserAssignedWorksQuery(tenantId);
            var result = await Send(query);
            return Ok(result);
        }
    }
}
