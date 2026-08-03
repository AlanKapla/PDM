using Business.Interfaces.Constants;
using Business.Interfaces.WebModels.CostEstimates;
using CQRS.CostEstimates.AddCostEstimateGroup;
using CQRS.CostEstimates.AddCostEstimateItem;
using CQRS.CostEstimates.CopyCostEstimate;
using CQRS.CostEstimates.CreateCostEstimate;
using CQRS.CostEstimates.DeleteCostEstimate;
using CQRS.CostEstimates.DeleteCostEstimateGroup;
using CQRS.CostEstimates.DeleteCostEstimateItem;
using CQRS.CostEstimates.ExportCostEstimate;
using CQRS.CostEstimates.GetCostEstimateDetails;
using CQRS.CostEstimates.GetCostEstimates;
using CQRS.CostEstimates.ReorderCostEstimateGroups;
using CQRS.CostEstimates.ReorderCostEstimateItems;
using CQRS.CostEstimates.ReorderCostEstimateItemChildren;
using CQRS.CostEstimates.ShareCostEstimate;
using CQRS.CostEstimates.UpdateCostEstimateShares;
using CQRS.CostEstimates.UpdateCostEstimate;
using CQRS.CostEstimates.MoveCostEstimateItem;
using CQRS.CostEstimates.RecalculateCostEstimate;
using CQRS.CostEstimates.UploadItemFiles;
using CQRS.CostEstimates.DeleteItemFile;
using CQRS.CostEstimates.ReplaceItemFiles;
using CQRS.CostEstimates.GenerateCostEstimateAIPreview;
using CQRS.CostEstimates.CreateCostEstimateFromAIPreview;
using CQRS.CostEstimates.GetAdditionalFields;
using CQRS.CostEstimates.AddAdditionalField;
using CQRS.CostEstimates.UpdateAdditionalField;
using CQRS.CostEstimates.DeleteAdditionalField;
using CQRS.CostEstimates.ReorderAdditionalFields;
using CQRS.CostEstimates.UpsertAdditionalFieldValue;
using CQRS.CostEstimates.UpdateItemBaseFields;
using CQRS.CostEstimates.SetItemIsSelected;
using CQRS.CostEstimates.UpdateGroupBaseFields;
using Business.Interfaces.WebModels.AI;
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
        [Authorize(Policy = PermissionCodes.ProjectEstimates)]
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
        [Authorize(Policy = PermissionCodes.ProjectEstimates)]
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
        /// Export cost estimate as XLSX file
        /// </summary>
        [HttpGet("{id:guid}/export/xlsx")]
        [Authorize(Policy = PermissionCodes.ProjectEstimates)]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> ExportXlsx(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid id)
        {
            ExportCostEstimateQuery query = new ExportCostEstimateQuery
            {
                TenantId = tenantId,
                ProjectId = projectId,
                CostEstimateId = id,
                Format = CostEstimateExportFormat.Xlsx
            };

            CostEstimateExportFile file = await Send(query);
            return File(file.Content, file.ContentType, file.FileName);
        }

        /// <summary>
        /// Export cost estimate as PDF file
        /// </summary>
        [HttpGet("{id:guid}/export/pdf")]
        [Authorize(Policy = PermissionCodes.ProjectEstimates)]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> ExportPdf(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid id)
        {
            ExportCostEstimateQuery query = new ExportCostEstimateQuery
            {
                TenantId = tenantId,
                ProjectId = projectId,
                CostEstimateId = id,
                Format = CostEstimateExportFormat.Pdf
            };

            CostEstimateExportFile file = await Send(query);
            return File(file.Content, file.ContentType, file.FileName);
        }

        /// <summary>
        /// Create new cost estimate with default schema
        /// </summary>
        /// <param name="tenantId">Tenant ID</param>
        /// <param name="projectId">Project ID</param>
        /// <param name="command">Name and optional description</param>
        /// <returns>Created cost estimate ID</returns>
        [HttpPost]
        [Authorize(Policy = PermissionCodes.ProjectEstimates)]
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
        /// Generuje podgląd kosztorysu przez AI na podstawie opisu inwestycji.
        /// Nie zapisuje niczego do bazy danych — zwraca podgląd do zatwierdzenia przez użytkownika.
        /// </summary>
        /// <param name="tenantId">Tenant ID</param>
        /// <param name="projectId">Project ID</param>
        /// <param name="request">Opis inwestycji</param>
        [HttpPost("generate-ai-preview")]
        [Authorize(Policy = PermissionCodes.ProjectEstimates)]
        [ProducesResponseType(typeof(AICostEstimatePreviewWeb), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GenerateCostEstimateAIPreview(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromBody] AICostEstimateRequestWeb request)
        {
            GenerateCostEstimateAIPreviewCommand command = new GenerateCostEstimateAIPreviewCommand
            {
                TenantId = tenantId,
                ProjectId = projectId,
                Request = request
            };
            return Ok(await Send(command));
        }

        /// <summary>
        /// Zapisuje kosztorys zatwierdzony przez użytkownika z podglądu wygenerowanego przez AI.
        /// Atomowo tworzy kosztorys z grupami, pozycjami i wartościami pól.
        /// Zwraca ID nowo utworzonego kosztorysu.
        /// </summary>
        /// <param name="tenantId">Tenant ID</param>
        /// <param name="projectId">Project ID</param>
        /// <param name="body">Nazwa, opis i podgląd AI</param>
        [HttpPost("create-from-ai-preview")]
        [Authorize(Policy = PermissionCodes.ProjectEstimates)]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CreateCostEstimateFromAIPreview(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromBody] CreateCostEstimateFromAIPreviewWeb body)
        {
            CreateCostEstimateFromAIPreviewCommand command = new CreateCostEstimateFromAIPreviewCommand
            {
                TenantId = tenantId,
                ProjectId = projectId,
                Name = body.Name,
                Description = body.Description,
                Preview = body.Preview
            };
            Guid id = await Send(command);
            return CreatedAtAction(
                nameof(GetCostEstimateDetails),
                new { tenantId, projectId, id },
                id);
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
        [Authorize(Policy = PermissionCodes.ProjectEstimates)]
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
        [Authorize(Policy = PermissionCodes.ProjectEstimates)]
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
        [Authorize(Policy = PermissionCodes.ProjectEstimates)]
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
        /// Dodaj pliki do pozycji (append). Nie wymaga fieldDefinitionId.
        /// Pliki są przypisane bezpośrednio do pozycji (CostEstimateItemFile).
        /// Dozwolone formaty: PDF, JPG. Max 50 MB na plik, max 10 plików.
        /// </summary>
        /// <param name="tenantId">Tenant ID</param>
        /// <param name="projectId">Project ID</param>
        /// <param name="id">Cost estimate ID</param>
        /// <param name="itemId">Item ID</param>
        /// <param name="files">Pliki do dodania</param>
        /// <returns>Lista ID utworzonych plików</returns>
        [HttpPost("{id:guid}/items/{itemId:guid}/item-files")]
        [Authorize(Policy = PermissionCodes.ProjectEstimates)]
        [ProducesResponseType(typeof(List<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [RequestSizeLimit(524_288_000)] // 500 MB total (10 files * 50 MB)
        public async Task<IActionResult> UploadItemFiles(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid id,
            [FromRoute] Guid itemId,
            [FromForm] List<IFormFile> files)
        {
            UploadItemFilesCommand command = new UploadItemFilesCommand
            {
                TenantId = tenantId,
                ProjectId = projectId,
                CostEstimateId = id,
                ItemId = itemId,
                Files = files
            };

            List<Guid> result = await Send(command);
            return Ok(result);
        }

        /// <summary>
        /// Zastąp wszystkie pliki pozycji (replace all).
        /// Soft-delete wszystkich istniejących plików + usunięcie blobów, następnie upload nowych.
        /// Pusta lista = usunięcie wszystkich plików.
        /// </summary>
        /// <param name="tenantId">Tenant ID</param>
        /// <param name="projectId">Project ID</param>
        /// <param name="id">Cost estimate ID</param>
        /// <param name="itemId">Item ID</param>
        /// <param name="files">Nowa lista plików (zastępuje istniejące)</param>
        /// <returns>Lista ID nowo utworzonych plików</returns>
        [HttpPut("{id:guid}/items/{itemId:guid}/item-files")]
        [Authorize(Policy = PermissionCodes.ProjectEstimates)]
        [ProducesResponseType(typeof(List<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [RequestSizeLimit(524_288_000)]
        public async Task<IActionResult> ReplaceItemFiles(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid id,
            [FromRoute] Guid itemId,
            [FromForm] List<IFormFile> files)
        {
            ReplaceItemFilesCommand command = new ReplaceItemFilesCommand
            {
                TenantId = tenantId,
                ProjectId = projectId,
                CostEstimateId = id,
                ItemId = itemId,
                Files = files
            };

            List<Guid> result = await Send(command);
            return Ok(result);
        }

        /// <summary>
        /// Usuń plik z pozycji (soft delete + usunięcie bloba).
        /// </summary>
        /// <param name="tenantId">Tenant ID</param>
        /// <param name="projectId">Project ID</param>
        /// <param name="id">Cost estimate ID</param>
        /// <param name="itemId">Item ID</param>
        /// <param name="fileId">File ID do usunięcia</param>
        /// <returns>No content</returns>
        [HttpDelete("{id:guid}/items/{itemId:guid}/item-files/{fileId:guid}")]
        [Authorize(Policy = PermissionCodes.ProjectEstimates)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeleteItemFile(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid id,
            [FromRoute] Guid itemId,
            [FromRoute] Guid fileId)
        {
            DeleteItemFileCommand command = new DeleteItemFileCommand
            {
                TenantId = tenantId,
                ProjectId = projectId,
                CostEstimateId = id,
                ItemId = itemId,
                FileId = fileId
            };

            await Send(command);
            return NoContent();
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
        [Authorize(Policy = PermissionCodes.ProjectEstimates)]
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
        [Authorize(Policy = PermissionCodes.ProjectEstimates)]
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
        [Authorize(Policy = PermissionCodes.ProjectEstimates)]
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
        [Authorize(Policy = PermissionCodes.ProjectEstimates)]
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
        [Authorize(Policy = PermissionCodes.ProjectEstimates)]
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
        [Authorize(Policy = PermissionCodes.ProjectEstimates)]
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
        /// Reorder child items (options or components) within a parent item
        /// Updates the Order property for specified child items
        /// </summary>
        /// <param name="tenantId">Tenant ID</param>
        /// <param name="projectId">Project ID</param>
        /// <param name="id">Cost estimate ID</param>
        /// <param name="parentItemId">Parent item ID containing the child items</param>
        /// <param name="command">List of child item IDs with new order values</param>
        /// <returns>No content</returns>
        [HttpPut("{id:guid}/items/{parentItemId:guid}/children/reorder")]
        [Authorize(Policy = PermissionCodes.ProjectEstimates)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> ReorderCostEstimateItemChildren(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid id,
            [FromRoute] Guid parentItemId,
            [FromBody] ReorderCostEstimateItemChildrenCommand command)
        {
            command = command with
            {
                CostEstimateId = id,
                ParentItemId = parentItemId,
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
        [Authorize(Policy = PermissionCodes.ProjectEstimates)]
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
        [Authorize(Policy = PermissionCodes.ProjectEstimates)]
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

        // ==================================================================================
        // ADDITIONAL FIELD VALUE OPERATIONS (nowa płaska struktura)
        // ==================================================================================

        /// <summary>
        /// Zapisz wartość pola dodatkowego dla grupy (upsert).
        /// Jeśli wartość istnieje — aktualizuje, jeśli nie — tworzy nową.
        /// </summary>
        /// <param name="tenantId">Tenant ID</param>
        /// <param name="projectId">Project ID</param>
        /// <param name="id">Cost estimate ID</param>
        /// <param name="groupId">Group ID</param>
        /// <param name="command">Dane wartości pola dodatkowego</param>
        /// <returns>ID wartości pola dodatkowego</returns>
        [HttpPatch("{id:guid}/groups/{groupId:guid}/additional-fields")]
        [Authorize(Policy = PermissionCodes.ProjectEstimates)]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpsertGroupAdditionalField(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid id,
            [FromRoute] Guid groupId,
            [FromBody] UpsertAdditionalFieldValueCommand command)
        {
            command = command with
            {
                CostEstimateId = id,
                GroupId = groupId,
                TenantId = tenantId,
                ProjectId = projectId
            };

            Guid valueId = await Send(command);
            return Ok(valueId);
        }

        /// <summary>
        /// Zapisz wartość pola dodatkowego dla pozycji (upsert).
        /// Jeśli wartość istnieje — aktualizuje, jeśli nie — tworzy nową.
        /// Automatycznie przelicza kosztorys po zapisie.
        /// </summary>
        /// <param name="tenantId">Tenant ID</param>
        /// <param name="projectId">Project ID</param>
        /// <param name="id">Cost estimate ID</param>
        /// <param name="itemId">Item ID</param>
        /// <param name="command">Dane wartości pola dodatkowego</param>
        /// <returns>ID wartości pola dodatkowego</returns>
        [HttpPatch("{id:guid}/items/{itemId:guid}/additional-fields")]
        [Authorize(Policy = PermissionCodes.ProjectEstimates)]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpsertItemAdditionalField(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid id,
            [FromRoute] Guid itemId,
            [FromBody] UpsertAdditionalFieldValueCommand command)
        {
            command = command with
            {
                CostEstimateId = id,
                ItemId = itemId,
                TenantId = tenantId,
                ProjectId = projectId
            };

            Guid valueId = await Send(command);
            return Ok(valueId);
        }

        // ==================================================================================
        // ITEM/GROUP BASE FIELDS UPDATE
        // ==================================================================================

        /// <summary>
        /// Zaktualizuj podstawowe pola pozycji kosztorysu (name, quantity, unit, price, vat).
        /// Tylko nie-null właściwości są aktualizowane.
        /// Jeśli zmieniono pole finansowe — automatycznie przelicza kosztorys.
        /// </summary>
        /// <param name="tenantId">Tenant ID</param>
        /// <param name="projectId">Project ID</param>
        /// <param name="id">Cost estimate ID</param>
        /// <param name="itemId">Item ID</param>
        /// <param name="command">Dane podstawowe pozycji do aktualizacji</param>
        /// <returns>No content</returns>
        [HttpPatch("{id:guid}/items/{itemId:guid}")]
        [Authorize(Policy = PermissionCodes.ProjectEstimates)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdateItemBaseFields(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid id,
            [FromRoute] Guid itemId,
            [FromBody] UpdateItemBaseFieldsCommand command)
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

        /// <summary>
        /// Zmień IsSelected dla pozycji/opcji/komponentu.
        /// Dla opcji: auto-deselect pozostałych opcji (exclusive).
        /// Dla pozycji/komponentu: zmiana checkboxa do sumowania.
        /// </summary>
        /// <param name="tenantId">Tenant ID</param>
        /// <param name="projectId">Project ID</param>
        /// <param name="id">Cost estimate ID</param>
        /// <param name="itemId">Item ID</param>
        /// <param name="command">IsSelected value</param>
        /// <returns>No content</returns>
        [HttpPatch("{id:guid}/items/{itemId:guid}/select")]
        [Authorize(Policy = PermissionCodes.ProjectEstimates)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> SetItemIsSelected(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid id,
            [FromRoute] Guid itemId,
            [FromBody] SetItemIsSelectedCommand command)
        {
            command = command with
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
        /// Zaktualizuj podstawowe pola grupy kosztorysu (name).
        /// Tylko nie-null właściwości są aktualizowane.
        /// </summary>
        /// <param name="tenantId">Tenant ID</param>
        /// <param name="projectId">Project ID</param>
        /// <param name="id">Cost estimate ID</param>
        /// <param name="groupId">Group ID</param>
        /// <param name="command">Dane podstawowe grupy do aktualizacji</param>
        /// <returns>No content</returns>
        [HttpPatch("{id:guid}/groups/{groupId:guid}")]
        [Authorize(Policy = PermissionCodes.ProjectEstimates)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdateGroupBaseFields(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid id,
            [FromRoute] Guid groupId,
            [FromBody] UpdateGroupBaseFieldsCommand command)
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

        // ==================================================================================
        // SHARE OPERATIONS
        // ==================================================================================

        /// <summary>
        /// Share a cost estimate with project members
        /// </summary>
        [HttpPost("{id:guid}/shares")]
        [Authorize(Policy = PermissionCodes.ProjectEstimates)]
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
        [Authorize(Policy = PermissionCodes.ProjectEstimates)]
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
        
        // ========================================================================
        // ADDITIONAL FIELDS (schema) ENDPOINTS
        // ========================================================================

        /// <summary>
        /// Pobierz wszystkie pola dodatkowe kosztorysu
        /// </summary>
        /// <param name="tenantId">Tenant ID</param>
        /// <param name="projectId">Project ID</param>
        /// <param name="id">Cost estimate ID</param>
        /// <returns>Lista pól dodatkowych posortowana po Order</returns>
        [HttpGet("{id:guid}/additional-fields")]
        [Authorize(Policy = PermissionCodes.ProjectEstimates)]
        [ProducesResponseType(typeof(List<CostEstimateAdditionalFieldWeb>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAdditionalFields(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid id)
        {
            GetAdditionalFieldsQuery query = new GetAdditionalFieldsQuery
            {
                TenantId = tenantId,
                ProjectId = projectId,
                CostEstimateId = id
            };

            List<CostEstimateAdditionalFieldWeb> result = await Send(query);
            return Ok(result);
        }

        /// <summary>
        /// Dodaj nowe pole dodatkowe do kosztorysu
        /// </summary>
        /// <param name="tenantId">Tenant ID</param>
        /// <param name="projectId">Project ID</param>
        /// <param name="id">Cost estimate ID</param>
        /// <param name="command">Dane nowego pola (nazwa, typ, opcjonalnie kolejność)</param>
        /// <returns>ID utworzonego pola dodatkowego</returns>
        [HttpPost("{id:guid}/additional-fields")]
        [Authorize(Policy = PermissionCodes.ProjectEstimates)]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AddAdditionalField(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid id,
            [FromBody] AddAdditionalFieldCommand command)
        {
            command = command with
            {
                TenantId = tenantId,
                ProjectId = projectId,
                CostEstimateId = id
            };

            Guid fieldId = await Send(command);
            return CreatedAtAction(nameof(GetCostEstimateDetails), new { tenantId, projectId, id }, fieldId);
        }

        /// <summary>
        /// Edytuj pole dodatkowe
        /// </summary>
        /// <param name="tenantId">Tenant ID</param>
        /// <param name="projectId">Project ID</param>
        /// <param name="id">Cost estimate ID</param>
        /// <param name="fieldId">Field ID</param>
        /// <param name="command">Dane do aktualizacji (tylko nie-null properties są aktualizowane)</param>
        /// <returns>No content</returns>
        [HttpPut("{id:guid}/additional-fields/{fieldId:guid}")]
        [Authorize(Policy = PermissionCodes.ProjectEstimates)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateAdditionalField(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid id,
            [FromRoute] Guid fieldId,
            [FromBody] UpdateAdditionalFieldCommand command)
        {
            command = command with
            {
                TenantId = tenantId,
                ProjectId = projectId,
                CostEstimateId = id,
                FieldId = fieldId
            };

            await Send(command);
            return NoContent();
        }

        /// <summary>
        /// Usuń pole dodatkowe
        /// </summary>
        /// <param name="tenantId">Tenant ID</param>
        /// <param name="projectId">Project ID</param>
        /// <param name="id">Cost estimate ID</param>
        /// <param name="fieldId">Field ID do usunięcia</param>
        /// <returns>No content</returns>
        [HttpDelete("{id:guid}/additional-fields/{fieldId:guid}")]
        [Authorize(Policy = PermissionCodes.ProjectEstimates)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteAdditionalField(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid id,
            [FromRoute] Guid fieldId)
        {
            DeleteAdditionalFieldCommand command = new DeleteAdditionalFieldCommand
            {
                TenantId = tenantId,
                ProjectId = projectId,
                CostEstimateId = id,
                FieldId = fieldId
            };

            await Send(command);
            return NoContent();
        }

        /// <summary>
        /// Zmień kolejność pól dodatkowych
        /// </summary>
        /// <param name="tenantId">Tenant ID</param>
        /// <param name="projectId">Project ID</param>
        /// <param name="id">Cost estimate ID</param>
        /// <param name="command">Lista ID pól w nowej kolejności</param>
        /// <returns>No content</returns>
        [HttpPost("{id:guid}/additional-fields/reorder")]
        [Authorize(Policy = PermissionCodes.ProjectEstimates)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ReorderAdditionalFields(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid id,
            [FromBody] ReorderAdditionalFieldsCommand command)
        {
            command = command with
            {
                TenantId = tenantId,
                ProjectId = projectId,
                CostEstimateId = id
            };

            await Send(command);
            return NoContent();
        }
    }
}
