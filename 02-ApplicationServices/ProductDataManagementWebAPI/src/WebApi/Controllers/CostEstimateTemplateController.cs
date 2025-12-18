using CQRS.CostEstimates.CreateCostEstimateTemplate;
using CQRS.CostEstimates.DeleteCostEstimateTemplate;
using CQRS.CostEstimates.GetCostEstimateTemplateDetails;
using CQRS.CostEstimates.GetCostEstimateTemplates;
using CQRS.CostEstimates.UpdateCostEstimateTemplate;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    /// <summary>
    /// Controller for managing cost estimate templates
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CostEstimateTemplateController : BaseApiController
    {
        public CostEstimateTemplateController(IMediator mediator) : base(mediator)
        {
        }

        /// <summary>
        /// Get all templates for current user
        /// </summary>
        /// <returns>List of templates</returns>
        [HttpGet]
        [ProducesResponseType(typeof(List<CostEstimateTemplateListItem>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTemplates()
        {
            return Ok(await Send(new GetCostEstimateTemplatesQuery()));
        }

        /// <summary>
        /// Get template details by ID
        /// </summary>
        /// <param name="id">Template ID</param>
        /// <returns>Template details with full structure</returns>
        [HttpGet]
        [Route("{id:guid}")]
        [ProducesResponseType(typeof(CostEstimateTemplateDetails), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetTemplateDetails([FromRoute] Guid id)
        {
            return Ok(await Send(new GetCostEstimateTemplateDetailsQuery(id)));
        }

        /// <summary>
        /// Create new template
        /// </summary>
        /// <param name="command">Template data</param>
        /// <returns>Created template ID</returns>
        [HttpPost]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateTemplate([FromBody] CreateCostEstimateTemplateCommand command)
        {
            var templateId = await Send(command);
            return CreatedAtAction(nameof(GetTemplateDetails), new { id = templateId }, templateId);
        }

        /// <summary>
        /// Update existing template
        /// </summary>
        /// <param name="id">Template ID</param>
        /// <param name="command">Updated template data</param>
        /// <returns>No content</returns>
        [HttpPut]
        [Route("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdateTemplate([FromRoute] Guid id, [FromBody] UpdateCostEstimateTemplateCommand command)
        {
            if (id != command.TemplateId)
            {
                return BadRequest("Template ID mismatch");
            }

            await Send(command);
            return NoContent();
        }

        /// <summary>
        /// Delete template (soft delete)
        /// </summary>
        /// <param name="id">Template ID</param>
        /// <returns>No content</returns>
        [HttpDelete]
        [Route("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeleteTemplate([FromRoute] Guid id)
        {
            await Send(new DeleteCostEstimateTemplateCommand(id));
            return NoContent();
        }
    }
}
