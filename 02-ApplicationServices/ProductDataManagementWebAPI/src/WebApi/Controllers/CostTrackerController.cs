using Business.Interfaces.Constants;
using Business.Interfaces.WebModels.CostTrackers;
using CQRS.CostTrackers.CreateTrackedCost;
using CQRS.CostTrackers.DeleteTrackedCost;
using CQRS.CostTrackers.GetCostTrackerByProject;
using CQRS.CostTrackers.UpdateTrackedCost;
using CQRS.CostTrackers.UpdateTrackerBudget;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    /// <summary>
    /// Controller for managing cost trackers and tracked costs
    /// </summary>
    [ApiController]
    [Route("api/tenants/{tenantId:guid}/projects/{projectId:guid}/cost-trackers")]
    public class CostTrackerController : BaseApiController
    {
        public CostTrackerController(IMediator mediator) : base(mediator)
        {
        }

        /// <summary>
        /// Get full cost tracker details for a project (all estimates aggregated)
        /// </summary>
        /// <param name="tenantId">Tenant ID</param>
        /// <param name="projectId">Project ID</param>
        /// <returns>Cost tracker details with project summary, per-estimate summaries and project-level additional costs</returns>
        [HttpGet("by-project")]
        [Authorize(Policy = PermissionCodes.ProjectResourcesReadSingle)]
        [ProducesResponseType(typeof(CostTrackerDetailsWeb), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetCostTrackerByProject(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId)
        {
            var query = new GetCostTrackerByProjectQuery() with
            {
                TenantId = tenantId,
                ProjectId = projectId
            };

            return Ok(await Send(query));
        }

        /// <summary>
        /// Create a tracked cost in a tracker
        /// </summary>
        /// <param name="tenantId">Tenant ID</param>
        /// <param name="projectId">Project ID</param>
        /// <param name="command">Tracked cost data with optional file attachments</param>
        /// <returns>Created tracked cost</returns>
        [HttpPost("costs")]
        [Authorize(Policy = PermissionCodes.ProjectResourcesWrite)]
        [RequestSizeLimit(52428800)]
        [RequestFormLimits(MultipartBodyLengthLimit = 52428800)]
        [ProducesResponseType(typeof(TrackedCostWeb), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CreateTrackedCost(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromForm] CreateTrackedCostCommand command)
        {
            command = command with
            {
                TenantId = tenantId,
                ProjectId = projectId,
            };

            return Ok(await Send(command));
        }

        /// <summary>
        /// Update a tracked cost (full replacement)
        /// </summary>
        /// <param name="tenantId">Tenant ID</param>
        /// <param name="projectId">Project ID</param>
        /// <param name="costId">Tracked cost ID</param>
        /// <param name="command">Updated tracked cost data with optional file attachments</param>
        /// <returns>Updated tracked cost</returns>
        [HttpPut("costs/{costId:guid}")]
        [Authorize(Policy = PermissionCodes.ProjectResourcesWrite)]
        [RequestSizeLimit(52428800)]
        [RequestFormLimits(MultipartBodyLengthLimit = 52428800)]
        [ProducesResponseType(typeof(TrackedCostWeb), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateTrackedCost(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid costId,
            [FromForm] UpdateTrackedCostCommand command)
        {
            command = command with
            {
                CostId = costId,
                TenantId = tenantId,
                ProjectId = projectId
            };

            return Ok(await Send(command));
        }

        /// <summary>
        /// Update budget fields (BudgetNet, BudgetGross) on a cost tracker
        /// </summary>
        /// <param name="tenantId">Tenant ID</param>
        /// <param name="projectId">Project ID</param>
        /// <param name="costTrackerId">Cost Tracker ID</param>
        /// <param name="command">Budget data</param>
        [HttpPut("{costTrackerId:guid}/budget")]
        [Authorize(Policy = PermissionCodes.ProjectResourcesWrite)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateTrackerBudget(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid costTrackerId,
            [FromBody] UpdateTrackerBudgetCommand command)
        {
            command = command with
            {
                CostTrackerId = costTrackerId,
                TenantId = tenantId,
                ProjectId = projectId
            };

            await Send(command);
            return NoContent();
        }

        /// <summary>
        /// Delete a tracked cost (soft delete)
        /// </summary>
        /// <param name="tenantId">Tenant ID</param>
        /// <param name="projectId">Project ID</param>
        /// <param name="costId">Tracked cost ID</param>
        [HttpDelete("costs/{costId:guid}")]
        [Authorize(Policy = PermissionCodes.ProjectResourcesWrite)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteTrackedCost(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid costId)
        {
            DeleteTrackedCostCommand command = new DeleteTrackedCostCommand
            {
                CostId = costId,
                TenantId = tenantId,
                ProjectId = projectId
            };

            await Send(command);
            return NoContent();
        }
    }
}
