using Business.Interfaces.Constants;
using Business.Interfaces.WebModels.AI;
using CQRS.AI.ParseCostDocument;
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
        /// Parsuje dokument kosztowy (JPG, PNG) przez GPT-4o Vision dla kosztu projektu (ProjectCost).
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
        /// Parsuje dokument kosztowy (JPG, PNG) przez GPT-4o Vision dla kosztu trackera (TrackedCost).
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

            string ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext is not (".jpg" or ".jpeg" or ".png"))
            {
                return BadRequest("Dozwolone formaty: JPG, PNG.");
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
