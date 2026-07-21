using Business.Interfaces.Constants;
using Business.Interfaces.Helpers;
using Business.Interfaces.WebModels.AI;
using CQRS.AI.AcceptAICostImportItem;
using CQRS.AI.AcceptAllAICostImportItems;
using CQRS.AI.GetAICostImportItem;
using CQRS.AI.GetPendingAICostImportCount;
using CQRS.AI.GetPendingAICostImportItems;
using CQRS.AI.ParseCostDocument;
using CQRS.AI.RejectAICostImportItem;
using CQRS.AI.SubmitAICostImportBatch;
using CQRS.AI.UpdateAICostImportItem;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/tenants/{tenantId:guid}/projects/{projectId:guid}/ai/cost")]
    public sealed class AICostController(IMediator mediator) : BaseApiController(mediator)
    {
        /// <summary>
        /// Parsuje dokument kosztowy (JPG, PNG, PDF) przez GPT-4o Vision dla kosztu projektu (ProjectCost).
        /// Zwraca sugestię danych kosztu do zatwierdzenia przez użytkownika.
        /// NIE zapisuje kosztu — tylko parsuje.
        /// </summary>
        [HttpPost("parse/project-cost")]
        [Authorize(Policy = PermissionCodes.ProjectCosts)]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(20_971_520)]
        [RequestFormLimits(MultipartBodyLengthLimit = 20_971_520)]
        [ProducesResponseType(typeof(ParsedCostDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> ParseProjectCostDocument(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            IFormFile file)
        {
            return await ParseDocumentInternal(tenantId, projectId, file, CostDocumentType.ProjectCost);
        }

        /// <summary>
        /// Parsuje dokument kosztowy (JPG, PNG, PDF) przez GPT-4o Vision dla kosztu trackera (TrackedCost).
        /// Zwraca sugestię danych kosztu do zatwierdzenia przez użytkownika.
        /// NIE zapisuje kosztu — tylko parsuje.
        /// </summary>
        [HttpPost("parse/tracked-cost")]
        [Authorize(Policy = PermissionCodes.ProjectDashboardTracker)]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(20_971_520)]
        [RequestFormLimits(MultipartBodyLengthLimit = 20_971_520)]
        [ProducesResponseType(typeof(ParsedCostDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> ParseTrackedCostDocument(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            IFormFile file)
        {
            return await ParseDocumentInternal(tenantId, projectId, file, CostDocumentType.TrackedCost);
        }

        [HttpPost("import/batch")]
        [Authorize]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(52_428_800)]
        [RequestFormLimits(MultipartBodyLengthLimit = 52_428_800)]
        [ProducesResponseType(typeof(AICostImportSubmitResultWeb), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SubmitImportBatch(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromForm] IFormFileCollection files,
            [FromForm] CostDocumentType costDocumentType,
            [FromForm] TrackedCostContextDto? trackedCostContext,
            CancellationToken cancellationToken)
        {
            if (files is null || files.Count == 0)
            {
                return BadRequest("At least 2 files are required.");
            }

            SubmitAICostImportBatchCommand command = new SubmitAICostImportBatchCommand
            {
                TenantId = tenantId,
                ProjectId = projectId,
                Files = files,
                CostDocumentType = costDocumentType,
                TrackedCostContext = trackedCostContext
            };

            AICostImportSubmitResultWeb result = await Send(command);
            return CreatedAtAction(
                nameof(GetPendingImportItems),
                new { tenantId, projectId },
                result);
        }

        [HttpGet("import/pending")]
        [Authorize(Policy = PermissionCodes.ProjectView)]
        [ProducesResponseType(typeof(IReadOnlyList<AICostImportItemWeb>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPendingImportItems(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            CancellationToken cancellationToken)
        {
            GetPendingAICostImportItemsQuery query = new GetPendingAICostImportItemsQuery
            {
                TenantId = tenantId,
                ProjectId = projectId
            };

            IReadOnlyList<AICostImportItemWeb> result = await Send(query);
            return Ok(result);
        }

        [HttpGet("import/pending/count")]
        [Authorize(Policy = PermissionCodes.ProjectView)]
        [ProducesResponseType(typeof(PendingAICostImportCountWeb), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPendingImportCount(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            CancellationToken cancellationToken)
        {
            GetPendingAICostImportCountQuery query = new GetPendingAICostImportCountQuery
            {
                TenantId = tenantId,
                ProjectId = projectId
            };

            PendingAICostImportCountWeb result = await Send(query);
            return Ok(result);
        }

        [HttpGet("import/pending/{itemId:guid}")]
        [Authorize(Policy = PermissionCodes.ProjectView)]
        [ProducesResponseType(typeof(AICostImportItemWeb), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetPendingImportItem(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid itemId,
            CancellationToken cancellationToken)
        {
            GetAICostImportItemQuery query = new GetAICostImportItemQuery
            {
                TenantId = tenantId,
                ProjectId = projectId,
                ItemId = itemId
            };

            AICostImportItemWeb result = await Send(query);
            return Ok(result);
        }

        [HttpPut("import/pending/{itemId:guid}")]
        [Authorize(Policy = PermissionCodes.ProjectView)]
        [ProducesResponseType(typeof(AICostImportItemWeb), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdatePendingImportItem(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid itemId,
            [FromBody] ParsedCostDto parsedData,
            CancellationToken cancellationToken)
        {
            UpdateAICostImportItemCommand command = new UpdateAICostImportItemCommand
            {
                TenantId = tenantId,
                ProjectId = projectId,
                ItemId = itemId,
                ParsedData = parsedData
            };

            AICostImportItemWeb result = await Send(command);
            return Ok(result);
        }

        [HttpPost("import/pending/{itemId:guid}/accept")]
        [Authorize(Policy = PermissionCodes.ProjectView)]
        [ProducesResponseType(typeof(AICostImportItemWeb), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AcceptPendingImportItem(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid itemId,
            CancellationToken cancellationToken)
        {
            AcceptAICostImportItemCommand command = new AcceptAICostImportItemCommand
            {
                TenantId = tenantId,
                ProjectId = projectId,
                ItemId = itemId
            };

            AICostImportItemWeb result = await Send(command);
            return Ok(result);
        }

        [HttpPost("import/pending/accept-all")]
        [Authorize(Policy = PermissionCodes.ProjectView)]
        [ProducesResponseType(typeof(AICostImportAcceptAllResultWeb), StatusCodes.Status200OK)]
        public async Task<IActionResult> AcceptAllPendingImportItems(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            CancellationToken cancellationToken)
        {
            AcceptAllAICostImportItemsCommand command = new AcceptAllAICostImportItemsCommand
            {
                TenantId = tenantId,
                ProjectId = projectId
            };

            AICostImportAcceptAllResultWeb result = await Send(command);
            return Ok(result);
        }

        [HttpDelete("import/pending/{itemId:guid}")]
        [Authorize(Policy = PermissionCodes.ProjectView)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RejectPendingImportItem(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid itemId,
            CancellationToken cancellationToken)
        {
            RejectAICostImportItemCommand command = new RejectAICostImportItemCommand
            {
                TenantId = tenantId,
                ProjectId = projectId,
                ItemId = itemId
            };

            await Send(command);
            return NoContent();
        }

        private async Task<IActionResult> ParseDocumentInternal(
            Guid tenantId,
            Guid projectId,
            IFormFile file,
            CostDocumentType costType)
        {
            if (file is null || file.Length == 0)
            {
                return BadRequest("Plik jest wymagany.");
            }

            FileContentValidator.FileValidationResult validation = FileContentValidator.Validate(file);
            if (!validation.IsSuccess)
            {
                return BadRequest(validation.FailureReason);
            }

            ParseCostDocumentQuery query = new()
            {
                TenantId = tenantId,
                ProjectId = projectId,
                File = file,
                CostType = costType
            };

            ParsedCostDto result = await Send(query);
            return Ok(result);
        }
    }
}
