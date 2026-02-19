using Business.Interfaces.Constants;
using Business.Interfaces.WebModels.ProjectCosts;
using CQRS.ProjectCosts.CreateProjectCost;
using CQRS.ProjectCosts.DeleteProjectCost;
using CQRS.ProjectCosts.ExtractProjectCostsFromFiles;
using CQRS.ProjectCosts.GetProjectCosts;
using CQRS.ProjectCosts.ShareProjectCosts;
using CQRS.ProjectCosts.UpdateCostShare;
using CQRS.ProjectCosts.UpdateProjectCost;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    /// <summary>
    /// Controller do zarządzania kosztami projektu
    /// </summary>
    [Route("api/tenants/{tenantId}/project/{projectId}/cost")]
    [ApiController]
    public class ProjectCostController(IMediator mediator) : BaseApiController(mediator)
    {
        /// <summary>
        /// Get project costs based on scope (All, Mine, Shared)
        /// </summary>
        /// <param name="tenantId">Tenant ID</param>
        /// <param name="projectId">Project ID</param>
        /// <param name="scope">Resource scope (All, Mine, Shared)</param>
        /// <returns>List of project costs</returns>
        [HttpGet("{scope}")]
        [Authorize(Policy = PermissionCodes.ProjectView)]
        public async Task<IActionResult> GetProjectCosts(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] ResourceScope scope)
        {
            var query = new GetProjectCostsQuery(tenantId, projectId, scope);
            var result = await Send(query);
            return Ok(result);
        }

        /// <summary>
        /// Tworzy nowy koszt projektu
        /// </summary>
        [HttpPost]
        [Authorize(Policy = PermissionCodes.ProjectResourcesWrite)]
        public async Task<IActionResult> CreateProjectCost(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromForm] CreateProjectCostCommand command)
        {
            command = command with
            {
                TenantId = tenantId,
                ProjectId = projectId
            };

            var costId = await Send(command);
            return Created(string.Empty, new { id = costId });
        }

        /// <summary>
        /// Aktualizuje istniejący koszt projektu
        /// </summary>
        [HttpPut("{costId}")]
        [Authorize(Policy = PermissionCodes.ProjectResourcesWrite)]
        public async Task<IActionResult> UpdateProjectCost(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid costId,
            [FromForm] UpdateProjectCostCommand command)
        {
            command = command with
            {
                TenantId = tenantId,
                ProjectId = projectId,
                CostId = costId
            };

            await Send(command);
            return NoContent();
        }

        /// <summary>
        /// Usuwa koszt projektu (soft delete)
        /// </summary>
        [HttpDelete("{costId}")]
        [Authorize(Policy = PermissionCodes.ProjectResourcesWrite)]
        public async Task<IActionResult> DeleteProjectCost(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid costId)
        {
            var command = new DeleteProjectCostCommand(tenantId, projectId, costId);
            await Send(command);
            return NoContent();
        }

        /// <summary>
        /// Udostępnia wiele kosztów wybranym członkom projektu (grupowe udostępnianie)
        /// </summary>
        [HttpPost("share")]
        [Authorize(Policy = PermissionCodes.ProjectResourcesShare)]
        public async Task<IActionResult> ShareProjectCosts(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromBody] ShareProjectCostsCommand command)
        {
            command = command with
            {
                TenantId = tenantId,
                ProjectId = projectId
            };

            await Send(command);
            return NoContent();
        }

        /// <summary>
        /// Aktualizuje udostępnienie pojedynczego kosztu - dodaje lub usuwa dostęp dla konkretnych użytkowników
        /// </summary>
        [HttpPut("{costId}/share")]
        [Authorize(Policy = PermissionCodes.ProjectResourcesShare)]
        public async Task<IActionResult> UpdateCostShare(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid costId,
            [FromBody] UpdateCostShareCommand command)
        {
            command = command with
            {
                TenantId = tenantId,
                ProjectId = projectId,
                CostId = costId
            };

            await Send(command);
            return NoContent();
        }

        /// <summary>
        /// Extract project costs from uploaded files using AI with Semantic Kernel
        /// Supports JPG and PDF files, max 50MB total
        /// Uses Azure OpenAI via Semantic Kernel and IAgentService to extract cost data from receipts, invoices, and documents
        /// Each file creates one ProjectCost entry with the document attached
        /// </summary>
        /// <param name="tenantId">Tenant ID</param>
        /// <param name="projectId">Project ID</param>
        /// <param name="files">Files to process (JPG/PDF, max 50MB total)</param>
        /// <returns>List of created project cost IDs and errors</returns>
        [HttpPost("extract-from-files")]
        [Authorize(Policy = PermissionCodes.ProjectResourcesWrite)]
        [ProducesResponseType(typeof(ExtractProjectCostsFromFilesResponseWeb), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [RequestSizeLimit(52428800)] // 50 MB
        public async Task<IActionResult> ExtractProjectCostsFromFiles(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromForm] List<IFormFile> files)
        {
            var command = new ExtractProjectCostsFromFilesCommand
            {
                TenantId = tenantId,
                ProjectId = projectId,
                Files = files
            };

            var result = await Send(command);
            return Ok(result);
        }
    }
}
