using CQRS.ProjectCosts.CreateProjectCost;
using CQRS.ProjectCosts.DeleteProjectCost;
using CQRS.ProjectCosts.GetProjectUserCosts;
using CQRS.ProjectCosts.GetSharedProjectCosts;
using CQRS.ProjectCosts.ShareProjectCost;
using CQRS.ProjectCosts.UpdateProjectCost;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Constants;

namespace WebApi.Controllers
{
    /// <summary>
    /// Controller do zarządzania kosztami projektu
    /// </summary>
    [Route("api/tenants/{tenantId}/projects/{projectId}/costs")]
    [ApiController]
    public class ProjectCostController(IMediator mediator) : BaseApiController(mediator)
    {
        /// <summary>
        /// Pobiera listę kosztów zalogowanego użytkownika w projekcie
        /// </summary>
        [HttpGet]
        [Authorize(Policy = Policies.ProjectEditor)]
        public async Task<IActionResult> GetProjectUserCosts(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId)
        {
            var query = new GetProjectUserCostsQuery
            {
                TenantId = tenantId,
                ProjectId = projectId
            };

            var result = await Send(query);
            return Ok(result);
        }

        /// <summary>
        /// Pobiera listę kosztów udostępnionych zalogowanemu użytkownikowi
        /// </summary>
        [HttpGet("shared")]
        [Authorize(Policy = Policies.ProjectViewer)]
        public async Task<IActionResult> GetSharedProjectCosts(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId)
        {
            var query = new GetSharedProjectCostsQuery
            {
                TenantId = tenantId,
                ProjectId = projectId
            };

            var result = await Send(query);
            return Ok(result);
        }

        /// <summary>
        /// Tworzy nowy koszt projektu
        /// </summary>
        [HttpPost]
        [Authorize(Policy = Policies.ProjectEditor)]
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
        [Authorize(Policy = Policies.ProjectEditor)]
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
        [Authorize(Policy = Policies.ProjectEditor)]
        public async Task<IActionResult> DeleteProjectCost(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid costId)
        {
            var command = new DeleteProjectCostCommand
            {
                TenantId = tenantId,
                ProjectId = projectId,
                CostId = costId
            };

            await Send(command);
            return NoContent();
        }

        /// <summary>
        /// Ustawia listę użytkowników, którym udostępniono koszt.
        /// Dodaje nowych użytkowników i usuwa tych, którzy nie są na liście.
        /// Pusta lista usuwa wszystkie udostępnienia.
        /// </summary>
        [HttpPost("{costId}/share")]
        [Authorize(Policy = Policies.ProjectEditor)]
        public async Task<IActionResult> ShareProjectCost(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid costId,
            [FromBody] ShareProjectCostCommand command)
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
