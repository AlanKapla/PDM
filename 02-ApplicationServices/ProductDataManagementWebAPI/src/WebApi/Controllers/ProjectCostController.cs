using Business.Interfaces.Constants;
using CQRS.ProjectCosts.CreateProjectCost;
using CQRS.ProjectCosts.DeleteProjectCost;
using CQRS.ProjectCosts.GetProjectUserCosts;
using CQRS.ProjectCosts.GetSharedProjectCosts;
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
        /// Pobiera listę kosztów zalogowanego użytkownika w projekcie
        /// </summary>
        [HttpGet]
        [Authorize(Policy = PermissionCodes.ProjectResourcesWrite)]
        public async Task<IActionResult> GetProjectUserCosts(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId)
        {
            var query = new GetProjectUserCostsQuery(tenantId, projectId);
            var result = await Send(query);
            return Ok(result);
        }

        /// <summary>
        /// Pobiera listę kosztów udostępnionych zalogowanemu użytkownikowi
        /// </summary>
        [HttpGet("shared")]
        [Authorize(Policy = PermissionCodes.ProjectResourcesReadShared)]
        public async Task<IActionResult> GetSharedProjectCosts(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId)
        {
            var query = new GetSharedProjectCostsQuery(tenantId, projectId);
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
        [Authorize(Policy = PermissionCodes.ProjectResourcesWrite)]
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
        [Authorize(Policy = PermissionCodes.ProjectResourcesWrite)]
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
    }
}
