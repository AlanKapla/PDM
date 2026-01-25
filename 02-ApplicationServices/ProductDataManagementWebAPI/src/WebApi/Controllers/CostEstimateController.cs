using Business.Interfaces.Constants;
using Business.Interfaces.WebModels.CostEstimates;
using CQRS.CostEstimates.CopyCostEstimate;
using CQRS.CostEstimates.CreateCostEstimate;
using CQRS.CostEstimates.DeleteCostEstimate;
using CQRS.CostEstimates.GetCostEstimateDetails;
using CQRS.CostEstimates.GetCostEstimates;
using CQRS.CostEstimates.UpdateCostEstimate;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/tenants/{tenantId:guid}/project/{projectId:guid}/cost-estimate")]
    public class CostEstimateController : BaseApiController
    {
        public CostEstimateController(IMediator mediator) : base(mediator)
        {
        }

        /// <summary>
        /// Get cost estimates based on scope (All, Mine, Shared)
        /// </summary>
        /// <param name="tenantId">Tenant ID</param>
        /// <param name="projectId">Project ID</param>
        /// <param name="scope">Resource scope (All, Mine, Shared)</param>
        /// <returns>List of cost estimates</returns>
        [HttpGet("{scope}")]
        [Authorize(Policy = PermissionCodes.ProjectView)]
        [ProducesResponseType(typeof(List<CostEstimateListItemWeb>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetCostEstimates(
            [FromRoute] Guid tenantId, 
            [FromRoute] Guid projectId,
            [FromRoute] ResourceScope scope)
        {
            var query = new GetCostEstimatesQuery(tenantId, projectId, scope);
            return Ok(await Send(query));
        }

        /// <summary>
        /// Get cost estimate details by ID
        /// Returns full hierarchy of groups and work scope items
        /// </summary>
        /// <param name="tenantId">Tenant ID</param>
        /// <param name="projectId">Project ID</param>
        /// <param name="id">Cost estimate ID</param>
        /// <returns>Cost estimate details with full data</returns>
        [HttpGet("details/{id:guid}")]
        [Authorize(Policy = PermissionCodes.ProjectResourcesReadSingle)]
        [ProducesResponseType(typeof(CostEstimateDetailsWeb), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetCostEstimateDetails(
            [FromRoute] Guid tenantId, 
            [FromRoute] Guid projectId, 
            [FromRoute] Guid id)
        {
            var query = new GetCostEstimateDetailsQuery(id) with
            {
                TenantId = tenantId,
                ProjectId = projectId
            };
            
            return Ok(await Send(query));
        }

        /// <summary>
        /// Create new cost estimate
        /// Can create empty cost estimate or with full hierarchy of groups and work scope items
        /// </summary>
        /// <param name="tenantId">Tenant ID</param>
        /// <param name="projectId">Project ID</param>
        /// <param name="command">Cost estimate data with optional groups hierarchy</param>
        /// <returns>Created cost estimate ID</returns>
        [HttpPost]
        [Authorize(Policy = PermissionCodes.ProjectResourcesWrite)]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreateCostEstimate(
            [FromRoute] Guid tenantId, 
            [FromRoute] Guid projectId, 
            [FromBody] CreateCostEstimateCommand command)
        {
            command = command with
            {
                TenantId = tenantId,
                ProjectId = projectId
            };

            var costEstimateId = await Send(command);
            return CreatedAtAction(nameof(GetCostEstimateDetails), 
                new { tenantId, projectId, id = costEstimateId }, costEstimateId);
        }

        /// <summary>
        /// Update existing cost estimate with full hierarchy
        /// Replaces all groups and work scope items with provided data
        /// Groups/items with Id will be updated, without Id will be created, missing will be deleted
        /// </summary>
        /// <param name="tenantId">Tenant ID</param>
        /// <param name="projectId">Project ID</param>
        /// <param name="id">Cost estimate ID</param>
        /// <param name="command">Updated cost estimate data with full hierarchy</param>
        /// <returns>No content</returns>
        [HttpPut("{id:guid}")]
        [Authorize(Policy = PermissionCodes.ProjectResourcesWrite)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdateCostEstimate(
            [FromRoute] Guid tenantId, 
            [FromRoute] Guid projectId, 
            [FromRoute] Guid id, 
            [FromBody] UpdateCostEstimateCommand command)
        {
            command = command with
            {
                CostEstimateId = id,
                TenantId = tenantId,
                ProjectId = projectId
            };

            await Send(command);
            return NoContent();
        }

        /// <summary>
        /// Delete cost estimate (soft delete)
        /// </summary>
        /// <param name="tenantId">Tenant ID</param>
        /// <param name="projectId">Project ID</param>
        /// <param name="id">Cost estimate ID</param>
        /// <returns>No content</returns>
        [HttpDelete("{id:guid}")]
        [Authorize(Policy = PermissionCodes.ProjectResourcesWrite)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeleteCostEstimate(
            [FromRoute] Guid tenantId, 
            [FromRoute] Guid projectId, 
            [FromRoute] Guid id)
        {
            var command = new DeleteCostEstimateCommand(id) with
            {
                TenantId = tenantId,
                ProjectId = projectId
            };
            
            await Send(command);
            return NoContent();
        }

        /// <summary>
        /// Copy cost estimate to other projects
        /// Creates deep copy of all groups and work scope items
        /// </summary>
        /// <param name="tenantId">Tenant ID</param>
        /// <param name="projectId">Source project ID</param>
        /// <param name="id">Cost estimate ID to copy</param>
        /// <param name="command">Target project IDs</param>
        /// <returns>List of created cost estimate IDs</returns>
        [HttpPost("{id:guid}/copy")]
        [Authorize(Policy = PermissionCodes.ProjectResourcesWrite)]
        [ProducesResponseType(typeof(List<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CopyCostEstimate(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid id,
            [FromBody] CopyCostEstimateCommand command)
        {
            command = command with
            {
                CostEstimateId = id,
                TenantId = tenantId,
                ProjectId = projectId
            };

            var result = await Send(command);
            return Ok(result);
        }
    }
}
