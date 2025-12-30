using CQRS.CostEstimates.CopyCostEstimate;
using CQRS.CostEstimates.CreateCostEstimate;
using CQRS.CostEstimates.DeleteCostEstimate;
using CQRS.CostEstimates.GetCostEstimateDetails;
using CQRS.CostEstimates.GetCostEstimates;
using CQRS.CostEstimates.UpdateCostEstimate;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Constants;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/tenants/{tenantId:guid}/projects/{projectId:guid}/cost-estimates")]
    public class CostEstimateController : BaseApiController
    {
        public CostEstimateController(IMediator mediator) : base(mediator)
        {
        }

        /// <summary>
        /// Get all cost estimates for project
        /// </summary>
        /// <param name="tenantId">Tenant ID</param>
        /// <param name="projectId">Project ID</param>
        /// <returns>List of cost estimates</returns>
        [HttpGet]
        [Authorize(Policy = Policies.ProjectEditor)]
        [ProducesResponseType(typeof(List<CostEstimateListItem>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetCostEstimates(
            [FromRoute] Guid tenantId, 
            [FromRoute] Guid projectId)
        {
            var query = new GetCostEstimatesQuery(projectId) with
            {
                TenantId = tenantId,
                ProjectId = projectId
            };
            
            return Ok(await Send(query));
        }

        /// <summary>
        /// Get cost estimate details by ID
        /// </summary>
        /// <param name="tenantId">Tenant ID</param>
        /// <param name="projectId">Project ID</param>
        /// <param name="id">Cost estimate ID</param>
        /// <returns>Cost estimate details with full data</returns>
        [HttpGet("{id:guid}")]
        [Authorize(Policy = Policies.ProjectEditor)]
        [ProducesResponseType(typeof(CostEstimateDetails), StatusCodes.Status200OK)]
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
        /// Create new empty cost estimate based on template
        /// </summary>
        /// <param name="tenantId">Tenant ID</param>
        /// <param name="projectId">Project ID</param>
        /// <param name="command">Cost estimate basic data</param>
        /// <returns>Created cost estimate ID</returns>
        [HttpPost]
        [Authorize(Policy = Policies.ProjectEditor)]
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
        /// Update existing cost estimate
        /// </summary>
        /// <param name="tenantId">Tenant ID</param>
        /// <param name="projectId">Project ID</param>
        /// <param name="id">Cost estimate ID</param>
        /// <param name="command">Updated cost estimate data</param>
        /// <returns>No content</returns>
        [HttpPut("{id:guid}")]
        [Authorize(Policy = Policies.ProjectEditor)]
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
        [Authorize(Policy = Policies.ProjectEditor)]
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
        /// Tenant admins can copy to any project in tenant
        /// Regular users can copy only to projects where they have Editor or Admin role
        /// </summary>
        /// <param name="tenantId">Tenant ID</param>
        /// <param name="projectId">Source project ID</param>
        /// <param name="id">Cost estimate ID to copy</param>
        /// <param name="command">Target project IDs</param>
        /// <returns>List of created cost estimate IDs</returns>
        [HttpPost("{id:guid}/copy")]
        [Authorize(Policy = Policies.ProjectEditor)]
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
