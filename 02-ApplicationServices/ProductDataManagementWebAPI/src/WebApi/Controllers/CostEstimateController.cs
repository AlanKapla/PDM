using Business.Interfaces.Constants;
using Business.Interfaces.WebModels.CostEstimates;
using CQRS.CostEstimates.AddCostEstimateGroup;
using CQRS.CostEstimates.AddCostEstimateItem;
using CQRS.CostEstimates.UpsertCostEstimateGroupField;
using CQRS.CostEstimates.UpsertCostEstimateItemField;
using CQRS.CostEstimates.CopyCostEstimate;
using CQRS.CostEstimates.CreateCostEstimate;
using CQRS.CostEstimates.DeleteCostEstimate;
using CQRS.CostEstimates.DeleteCostEstimateGroup;
using CQRS.CostEstimates.DeleteCostEstimateItem;
using CQRS.CostEstimates.GetCostEstimateDetails;
using CQRS.CostEstimates.GetCostEstimates;
using CQRS.CostEstimates.ReorderCostEstimateGroups;
using CQRS.CostEstimates.ReorderCostEstimateItems;
using CQRS.CostEstimates.ShareCostEstimate;
using CQRS.CostEstimates.UpdateCostEstimateShares;
using CQRS.CostEstimates.UpdateCostEstimate;
using CQRS.CostEstimates.MoveCostEstimateItem;
using CQRS.CostEstimates.RecalculateCostEstimate;
using CQRS.CostEstimates.UploadCostEstimateFieldFiles;
using Entities.Models.CostEstimates;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/tenants/{tenantId:guid}/projects/{projectId:guid}/cost-estimate")]
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
            var query = new GetCostEstimatesQuery
            {
                TenantId = tenantId,
                ProjectId = projectId,
                Scope = scope
            };
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
            var query = new GetCostEstimateDetailsQuery
            {
                TenantId = tenantId,
                ProjectId = projectId,
                CostEstimateId = id
            };

            return Ok(await Send(query));
        }

        /// <summary>
        /// Create new cost estimate based on selected template
        /// </summary>
        /// <param name="tenantId">Tenant ID</param>
        /// <param name="projectId">Project ID</param>
        /// <param name="command">Template, currency, name and optional description</param>
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
        /// Update cost estimate metadata (name and description)
        /// </summary>
        /// <param name="tenantId">Tenant ID</param>
        /// <param name="projectId">Project ID</param>
        /// <param name="id">Cost estimate ID</param>
        /// <param name="command">Updated name and description</param>
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
            var command = new DeleteCostEstimateCommand
            {
                TenantId = tenantId,
                ProjectId = projectId,
                CostEstimateId = id
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

        /// <summary>
        /// Replace all files on a cost estimate item field of type Files (ItemSystemFiles)
        /// Deletes ALL existing files (DB + Blob Storage) and uploads new ones.
        /// If the field value does not yet exist on the item, it will be created automatically.
        /// Sending empty files list clears all files from the field.
        /// Allowed formats: PDF, JPG. Max file size: 50 MB per file, max 10 files per request.
        /// </summary>
        /// <param name="tenantId">Tenant ID</param>
        /// <param name="projectId">Project ID</param>
        /// <param name="id">Cost estimate ID</param>
        /// <param name="itemId">Cost estimate item ID</param>
        /// <param name="fieldDefinitionId">Field definition ID (must be of type ItemSystemFiles)</param>
        /// <param name="files">New files to upload (replaces all existing)</param>
        /// <returns>List of created file IDs</returns>
        [HttpPost("{id:guid}/items/{itemId:guid}/files")]
        [Authorize(Policy = PermissionCodes.ProjectResourcesWrite)]
        [ProducesResponseType(typeof(List<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [RequestSizeLimit(524_288_000)] // 500 MB total (10 files * 50 MB)
        public async Task<IActionResult> UploadCostEstimateFieldFiles(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid id,
            [FromRoute] Guid itemId,
            [FromForm] Guid fieldDefinitionId,
            [FromForm] List<IFormFile> files)
        {
            var command = new UploadCostEstimateFieldFilesCommand
            {
                TenantId = tenantId,
                ProjectId = projectId,
                CostEstimateId = id,
                ItemId = itemId,
                FieldDefinitionId = fieldDefinitionId,
                Files = files
            };

            var result = await Send(command);
            return Ok(result);
        }

        // ==================================================================================
        // GROUP OPERATIONS
        // ==================================================================================

        /// <summary>
        /// Add a new group to a cost estimate
        /// Creates the group with empty field values based on the template definition
        /// </summary>
        /// <param name="tenantId">Tenant ID</param>
        /// <param name="projectId">Project ID</param>
        /// <param name="id">Cost estimate ID</param>
        /// <param name="command">Group data (parent group, order)</param>
        /// <returns>Created group ID and field values with empty defaults</returns>
        [HttpPost("{id:guid}/groups")]
        [Authorize(Policy = PermissionCodes.ProjectResourcesWrite)]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> AddCostEstimateGroup(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid id,
            [FromBody] AddCostEstimateGroupCommand command)
        {
            command = command with
            {
                CostEstimateId = id,
                TenantId = tenantId,
                ProjectId = projectId
            };

            var result = await Send(command);
            return CreatedAtAction(nameof(GetCostEstimateDetails), new { tenantId, projectId, id }, result);
        }

        /// <summary>
        /// Delete a group from a cost estimate (soft delete)
        /// Deletes the group, all child groups and their items
        /// </summary>
        /// <param name="tenantId">Tenant ID</param>
        /// <param name="projectId">Project ID</param>
        /// <param name="id">Cost estimate ID</param>
        /// <param name="groupId">Group ID to delete</param>
        /// <returns>No content</returns>
        [HttpDelete("{id:guid}/groups/{groupId:guid}")]
        [Authorize(Policy = PermissionCodes.ProjectResourcesWrite)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeleteCostEstimateGroup(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid id,
            [FromRoute] Guid groupId)
        {
            var command = new DeleteCostEstimateGroupCommand
            {
                TenantId = tenantId,
                ProjectId = projectId,
                CostEstimateId = id,
                GroupId = groupId
            };

            await Send(command);
            return NoContent();
        }

        /// <summary>
        /// Reorder groups within a cost estimate
        /// Updates the Order property for specified groups
        /// </summary>
        /// <param name="tenantId">Tenant ID</param>
        /// <param name="projectId">Project ID</param>
        /// <param name="id">Cost estimate ID</param>
        /// <param name="command">List of group IDs with new order values</param>
        /// <returns>No content</returns>
        [HttpPut("{id:guid}/groups/reorder")]
        [Authorize(Policy = PermissionCodes.ProjectResourcesWrite)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> ReorderCostEstimateGroups(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid id,
            [FromBody] ReorderCostEstimateGroupsCommand command)
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

        // ==================================================================================
        // ITEM OPERATIONS
        // ==================================================================================

        /// <summary>
        /// Add a new item to a cost estimate group
        /// Creates the item with empty field values based on the template definition
        /// </summary>
        /// <param name="tenantId">Tenant ID</param>
        /// <param name="projectId">Project ID</param>
        /// <param name="id">Cost estimate ID</param>
        /// <param name="command">Item data (group, parent item, relation type, order)</param>
        /// <returns>Created item ID and field values with empty defaults</returns>
        [HttpPost("{id:guid}/items")]
        [Authorize(Policy = PermissionCodes.ProjectResourcesWrite)]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> AddCostEstimateItem(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid id,
            [FromBody] AddCostEstimateItemCommand command)
        {
            command = command with
            {
                CostEstimateId = id,
                TenantId = tenantId,
                ProjectId = projectId
            };

            var result = await Send(command);
            return CreatedAtAction(nameof(GetCostEstimateDetails), new { tenantId, projectId, id }, result);
        }

        /// <summary>
        /// Delete an item from a cost estimate (soft delete)
        /// Deletes the item and all its child items (options, components)
        /// </summary>
        /// <param name="tenantId">Tenant ID</param>
        /// <param name="projectId">Project ID</param>
        /// <param name="id">Cost estimate ID</param>
        /// <param name="itemId">Item ID to delete</param>
        /// <returns>No content</returns>
        [HttpDelete("{id:guid}/items/{itemId:guid}")]
        [Authorize(Policy = PermissionCodes.ProjectResourcesWrite)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeleteCostEstimateItem(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid id,
            [FromRoute] Guid itemId)
        {
            var command = new DeleteCostEstimateItemCommand
            {
                TenantId = tenantId,
                ProjectId = projectId,
                CostEstimateId = id,
                ItemId = itemId
            };

            await Send(command);
            return NoContent();
        }

        /// <summary>
        /// Reorder items within a cost estimate group
        /// Updates the Order property for specified items
        /// </summary>
        /// <param name="tenantId">Tenant ID</param>
        /// <param name="projectId">Project ID</param>
        /// <param name="id">Cost estimate ID</param>
        /// <param name="groupId">Group ID containing the items</param>
        /// <param name="command">List of item IDs with new order values</param>
        /// <returns>No content</returns>
        [HttpPut("{id:guid}/groups/{groupId:guid}/items/reorder")]
        [Authorize(Policy = PermissionCodes.ProjectResourcesWrite)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> ReorderCostEstimateItems(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid id,
            [FromRoute] Guid groupId,
            [FromBody] ReorderCostEstimateItemsCommand command)
        {
            command = command with
            {
                CostEstimateId = id,
                GroupId = groupId,
                TenantId = tenantId,
                ProjectId = projectId
            };

            await Send(command);
            return NoContent();
        }

        /// <summary>
        /// Move an item from one group to another
        /// Only changes the GroupId — does not affect order or child structure
        /// Child items (options, components) are moved together with the parent
        /// </summary>
        /// <param name="tenantId">Tenant ID</param>
        /// <param name="projectId">Project ID</param>
        /// <param name="id">Cost estimate ID</param>
        /// <param name="itemId">Item ID to move</param>
        /// <param name="command">Target group ID</param>
        /// <returns>No content</returns>
        [HttpPatch("{id:guid}/items/{itemId:guid}/move")]
        [Authorize(Policy = PermissionCodes.ProjectResourcesWrite)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> MoveCostEstimateItem(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid id,
            [FromRoute] Guid itemId,
            [FromBody] MoveCostEstimateItemCommand command)
        {
            command = command with
            {
                CostEstimateId = id,
                ItemId = itemId,
                TenantId = tenantId,
                ProjectId = projectId
            };

            await Send(command);
            return NoContent();
        }

        // ==================================================================================
        // CALCULATION OPERATIONS
        // ==================================================================================

        /// <summary>
        /// Recalculate all totals (Net, Gross, VAT) for a cost estimate
        /// Recalculates item values, group totals and cost estimate totals based on template formulas
        /// </summary>
        /// <param name="tenantId">Tenant ID</param>
        /// <param name="projectId">Project ID</param>
        /// <param name="id">Cost estimate ID</param>
        /// <returns>No content</returns>
        [HttpPost("{id:guid}/recalculate")]
        [Authorize(Policy = PermissionCodes.ProjectResourcesWriteShared)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> RecalculateCostEstimate(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid id)
        {
            var command = new RecalculateCostEstimateCommand
            {
                TenantId = tenantId,
                ProjectId = projectId,
                CostEstimateId = id
            };

            await Send(command);
            return NoContent();
        }

        // ==================================================================================
        // FIELD OPERATIONS (autosave)
        // ==================================================================================

        /// <summary>
        /// Add or update a group field value (autosave).
        /// When FieldValueId is null a new field value is created (FieldDefinitionId is required).
        /// When FieldValueId is provided the existing field value is updated.
        /// </summary>
        /// <param name="tenantId">Tenant ID</param>
        /// <param name="projectId">Project ID</param>
        /// <param name="id">Cost estimate ID</param>
        /// <param name="groupId">Group ID</param>
        /// <param name="command">Field value data (FieldValueId null = add, non-null = update)</param>
        /// <returns>Field value ID</returns>
        [HttpPatch("{id:guid}/groups/{groupId:guid}/fields")]
        [Authorize(Policy = PermissionCodes.ProjectResourcesWriteShared)]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpsertCostEstimateGroupField(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid id,
            [FromRoute] Guid groupId,
            [FromBody] UpsertCostEstimateGroupFieldCommand command)
        {
            command = command with
            {
                CostEstimateId = id,
                GroupId = groupId,
                TenantId = tenantId,
                ProjectId = projectId
            };

            var fieldValueId = await Send(command);
            return Ok(fieldValueId);
        }

        /// <summary>
        /// Add or update an item field value (autosave).
        /// When FieldValueId is null a new field value is created (FieldDefinitionId is required).
        /// When FieldValueId is provided the existing field value is updated.
        /// Works for main items, options, and components.
        /// Does not trigger recalculation — call POST /{id}/recalculate separately.
        /// </summary>
        /// <param name="tenantId">Tenant ID</param>
        /// <param name="projectId">Project ID</param>
        /// <param name="id">Cost estimate ID</param>
        /// <param name="itemId">Item ID (main item, option, or component)</param>
        /// <param name="command">Field value data (FieldValueId null = add, non-null = update)</param>
        /// <returns>Field value ID</returns>
        [HttpPatch("{id:guid}/items/{itemId:guid}/fields")]
        [Authorize(Policy = PermissionCodes.ProjectResourcesWriteShared)]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpsertCostEstimateItemField(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid id,
            [FromRoute] Guid itemId,
            [FromBody] UpsertCostEstimateItemFieldCommand command)
        {
            command = command with
            {
                CostEstimateId = id,
                ItemId = itemId,
                TenantId = tenantId,
                ProjectId = projectId
            };

            var fieldValueId = await Send(command);
            return Ok(fieldValueId);
        }

        // ==================================================================================
        // SHARE OPERATIONS
        // ==================================================================================

        /// <summary>
        /// Share a cost estimate with project members
        /// </summary>
        [HttpPost("{id:guid}/shares")]
        [Authorize(Policy = PermissionCodes.ProjectResourcesShare)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> ShareCostEstimate(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid id,
            [FromBody] ShareCostEstimateRequestWeb body)
        {
            var command = new ShareCostEstimateCommand
            {
                TenantId = tenantId,
                ProjectId = projectId,
                CostEstimateId = id,
                ShareWithUserIds = body.UserIds
            };

            await Send(command);
            return NoContent();
        }

        /// <summary>
        /// Sets the desired share state for a cost estimate.
        /// Users in the list gain access (if not already), users missing from the list lose access.
        /// Sends notifications to affected users.
        /// </summary>
        [HttpPut("{id:guid}/shares")]
        [Authorize(Policy = PermissionCodes.ProjectResourcesShare)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdateCostEstimateShares(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid id,
            [FromBody] UpdateCostEstimateSharesRequestWeb body)
        {
            var command = new UpdateCostEstimateSharesCommand
            {
                TenantId = tenantId,
                ProjectId = projectId,
                CostEstimateId = id,
                UserIds = body.UserIds
            };

            await Send(command);
            return NoContent();
        }
    }
}
