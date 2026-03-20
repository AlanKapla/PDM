using Business.Interfaces.WebModels.CostEstimateTemplates;
using CQRS.CostEstimateTemplates.ApproveTemplateVersion;
using CQRS.CostEstimateTemplates.CreateCostEstimateTemplate;
using CQRS.CostEstimateTemplates.DeleteTemplateVersion;
using CQRS.CostEstimateTemplates.GetApprovedTemplateVersions;
using CQRS.CostEstimateTemplates.GetCostEstimateTemplateDetails;
using CQRS.CostEstimateTemplates.GetCostEstimateTemplates;
using CQRS.CostEstimateTemplates.GetCostEstimateTemplateVersions;
using CQRS.CostEstimateTemplates.GetFieldTypeConfigurations;
using CQRS.CostEstimateTemplates.GetTemplateVersionStructure;
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
        /// Optionally view a specific template version for comparison/history purposes
        /// </summary>
        /// <param name="id">Template ID</param>
        /// <param name="versionId">Optional: Version ID to view a specific template version</param>
        /// <returns>Template details with full structure</returns>
        [HttpGet]
        [Route("{id:guid}")]
        [ProducesResponseType(typeof(CostEstimateTemplateDetailsWeb), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetTemplateDetails([FromRoute] Guid id, [FromQuery] Guid? versionId = null)
        {
            var query = new GetCostEstimateTemplateDetailsQuery(id) with
            {
                VersionId = versionId
            };
            
            return Ok(await Send(query));
        }

        /// <summary>
        /// Get full version structure with all fields and configuration
        /// Returns complete structure needed to create a cost estimate
        /// </summary>
        /// <param name="templateId">Template ID</param>
        /// <param name="versionId">Version ID</param>
        /// <returns>Full version structure</returns>
        [HttpGet]
        [Route("{templateId:guid}/versions/{versionId:guid}/structure")]
        [ProducesResponseType(typeof(CostEstimateTemplateVersionStructureWeb), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetTemplateVersionStructure([FromRoute] Guid templateId, [FromRoute] Guid versionId)
        {
            return Ok(await Send(new GetTemplateVersionStructureQuery(templateId, versionId)));
        }

        /// <summary>
        /// Get version history for template
        /// </summary>
        /// <param name="id">Template ID</param>
        /// <returns>List of version history items</returns>
        [HttpGet]
        [Route("{id:guid}/versions")]
        [ProducesResponseType(typeof(List<CostEstimateTemplateVersionHistoryItemWeb>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetTemplateVersionHistory([FromRoute] Guid id)
        {
            return Ok(await Send(new GetCostEstimateTemplateVersionsQuery(id)));
        }

        /// <summary>
        /// Get all approved versions for current user's templates (for cost estimate creation)
        /// </summary>
        /// <returns>List of all approved versions from all user's templates</returns>
        [HttpGet]
        [Route("approved-versions")]
        [ProducesResponseType(typeof(List<ApprovedTemplateVersionItemWeb>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllApprovedVersions()
        {
            return Ok(await Send(new GetApprovedTemplateVersionsQuery()));
        }

        /// <summary>
        /// Get approved versions for specific template (for template history view)
        /// </summary>
        /// <param name="id">Template ID</param>
        /// <returns>List of approved versions for the template</returns>
        [HttpGet]
        [Route("{id:guid}/approved-versions")]
        [ProducesResponseType(typeof(List<ApprovedTemplateVersionItemWeb>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetTemplateApprovedVersions([FromRoute] Guid id)
        {
            var allVersions = await Send(new GetApprovedTemplateVersionsQuery());
            var templateVersions = allVersions.Where(v => v.TemplateId == id).ToList();
            return Ok(templateVersions);
        }

        /// <summary>
        /// Approve a template version
        /// </summary>
        /// <param name="templateId">Template ID</param>
        /// <param name="versionId">Version ID</param>
        /// <returns>No content</returns>
        [HttpPost]
        [Route("{templateId:guid}/versions/{versionId:guid}/approve")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> ApproveTemplateVersion([FromRoute] Guid templateId, [FromRoute] Guid versionId)
        {
            await Send(new ApproveTemplateVersionCommand(templateId, versionId));
            return NoContent();
        }

        /// <summary>
        /// Delete a template version
        /// Remaining versions are renumbered based on creation date
        /// Cannot delete if version is used by any cost estimates
        /// Cannot delete the only version (delete template instead)
        /// </summary>
        /// <param name="templateId">Template ID</param>
        /// <param name="versionId">Version ID</param>
        /// <returns>No content</returns>
        [HttpDelete]
        [Route("{templateId:guid}/versions/{versionId:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeleteTemplateVersion([FromRoute] Guid templateId, [FromRoute] Guid versionId)
        {
            await Send(new DeleteTemplateVersionCommand(templateId, versionId));
            return NoContent();
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
    }
}
