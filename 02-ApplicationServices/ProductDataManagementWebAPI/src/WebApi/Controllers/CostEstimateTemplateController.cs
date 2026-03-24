using Business.Interfaces.WebModels.CostEstimateTemplates;
using CQRS.CostEstimateTemplates.CreateCostEstimateTemplate;
using CQRS.CostEstimateTemplates.CreateCostEstimateTemplateFromDefault;
using CQRS.CostEstimateTemplates.DeleteCostEstimateTemplate;
using CQRS.CostEstimateTemplates.DuplicateCostEstimateTemplate;
using CQRS.CostEstimateTemplates.GetCostEstimateTemplateDetails;
using CQRS.CostEstimateTemplates.GetCostEstimateTemplates;
using CQRS.CostEstimateTemplates.GetDefaultCostEstimateTemplateDetails;
using CQRS.CostEstimateTemplates.GetDefaultCostEstimateTemplates;
using CQRS.CostEstimateTemplates.GetFieldTypeConfigurations;
using CQRS.CostEstimateTemplates.UpdateCostEstimateTemplate;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    /// <summary>
    /// Controller for managing cost estimate templates
    /// </summary>
    [ApiController]
    [Route("api/cost-estimate-template")]
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
        [ProducesResponseType(typeof(List<CostEstimateTemplateListItemWeb>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTemplates()
        {
            return Ok(await Send(new GetCostEstimateTemplatesQuery()));
        }
        
        /// <summary>
        /// Get field type configurations
        /// Returns dictionary of field type metadata grouped by FieldScope
        /// </summary>
        /// <returns>Dictionary: FieldScope -> FieldTypeConfig[]</returns>
        [HttpGet]
        [Route("field-type-configurations")]
        [ProducesResponseType(typeof(Dictionary<int, CostEstimateFieldTypeConfigWeb[]>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetFieldTypeConfigurations()
        {
            return Ok(await Send(new GetFieldTypeConfigurationsQuery()));
        }

        /// <summary>
        /// Get template details by ID
        /// </summary>
        /// <param name="id">Template ID</param>
        /// <returns>Template details with full structure</returns>
        [HttpGet]
        [Route("{id:guid}")]
        [ProducesResponseType(typeof(CostEstimateTemplateDetailsWeb), StatusCodes.Status200OK)]
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

        /// <summary>
        /// Duplicate a template with full structure
        /// </summary>
        [HttpPost]
        [Route("{id:guid}/duplicate")]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DuplicateTemplate([FromRoute] Guid id, [FromBody] DuplicateCostEstimateTemplateCommand command)
        {
            var newTemplateId = await Send(command with { SourceTemplateId = id });
            return CreatedAtAction(nameof(GetTemplateDetails), new { id = newTemplateId }, newTemplateId);
        }

        // ===== DEFAULT (SYSTEM) TEMPLATES =====

        /// <summary>
        /// Get list of all available default (system) templates
        /// </summary>
        [HttpGet]
        [Route("defaults")]
        [ProducesResponseType(typeof(List<DefaultCostEstimateTemplateListItemWeb>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDefaultTemplates()
        {
            return Ok(await Send(new GetDefaultCostEstimateTemplatesQuery()));
        }

        /// <summary>
        /// Get full structure of a default template by slug
        /// </summary>
        [HttpGet]
        [Route("defaults/{slug}")]
        [ProducesResponseType(typeof(CostEstimateTemplateStructureWeb), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetDefaultTemplateDetails([FromRoute] string slug)
        {
            return Ok(await Send(new GetDefaultCostEstimateTemplateDetailsQuery(slug)));
        }

        /// <summary>
        /// Create a new user template from a default template
        /// </summary>
        [HttpPost]
        [Route("defaults/{slug}")]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CreateTemplateFromDefault([FromRoute] string slug, [FromBody] CreateCostEstimateTemplateFromDefaultCommand command)
        {
            var newTemplateId = await Send(command with { Slug = slug });
            return CreatedAtAction(nameof(GetTemplateDetails), new { id = newTemplateId }, newTemplateId);
        }
    }
}
