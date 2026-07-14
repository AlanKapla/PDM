using Business.Interfaces.Constants;
using Business.Interfaces.WebModels.ProjectCosts;
using CQRS.ProjectCosts.ApproveProjectCost;
using CQRS.ProjectCosts.CreateProjectCost;
using CQRS.ProjectCosts.DeleteProjectCost;
using CQRS.ProjectCosts.GetProjectCosts;
using CQRS.ProjectCosts.RejectProjectCost;
using CQRS.ProjectCosts.SubmitProjectCostForApproval;
using CQRS.ProjectCosts.UpdateProjectCost;
using CQRS.ProjectCosts.WithdrawProjectCost;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [Route("api/tenants/{tenantId}/projects/{projectId}/cost")]
    [ApiController]
    public class ProjectCostController(IMediator mediator) : BaseApiController(mediator)
    {
        [HttpGet("{scope}")]
        [Authorize(Policy = PermissionCodes.ProjectCosts)]
        public async Task<IActionResult> GetProjectCosts(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] ResourceScope scope)
        {
            GetProjectCostsQuery query = new GetProjectCostsQuery
            {
                TenantId = tenantId,
                ProjectId = projectId,
                Scope = scope
            };
            IEnumerable<ProjectCostListItemWeb> result = await Send(query);
            return Ok(result);
        }

        [HttpPost]
        [Authorize(Policy = PermissionCodes.ProjectCosts)]
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

            ProjectCostListItemWeb result = await Send(command);
            return Created(string.Empty, result);
        }

        [HttpPut("{costId}")]
        [Authorize(Policy = PermissionCodes.ProjectCosts)]
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

            ProjectCostListItemWeb result = await Send(command);
            return Ok(result);
        }

        [HttpDelete("{costId}")]
        [Authorize(Policy = PermissionCodes.ProjectCosts)]
        public async Task<IActionResult> DeleteProjectCost(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid costId)
        {
            DeleteProjectCostCommand command = new DeleteProjectCostCommand
            {
                TenantId = tenantId,
                ProjectId = projectId,
                CostId = costId
            };
            await Send(command);
            return NoContent();
        }

        /// <summary>
        /// Skierowanie kosztu do akceptacji (właściciel lub admin, Draft → PendingApproval)
        /// </summary>
        [HttpPost("{costId}/submit")]
        [Authorize(Policy = PermissionCodes.ProjectCosts)]
        public async Task<IActionResult> SubmitForApproval(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid costId)
        {
            SubmitProjectCostForApprovalCommand command = new SubmitProjectCostForApprovalCommand
            {
                TenantId = tenantId,
                ProjectId = projectId,
                CostId = costId
            };
            ProjectCostListItemWeb result = await Send(command);
            return Ok(result);
        }

        /// <summary>
        /// Wycofanie kosztu z akceptacji (właściciel lub admin, PendingApproval → Draft)
        /// </summary>
        [HttpPost("{costId}/withdraw")]
        [Authorize(Policy = PermissionCodes.ProjectCosts)]
        public async Task<IActionResult> WithdrawFromApproval(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid costId)
        {
            WithdrawProjectCostCommand command = new WithdrawProjectCostCommand
            {
                TenantId = tenantId,
                ProjectId = projectId,
                CostId = costId
            };
            ProjectCostListItemWeb result = await Send(command);
            return Ok(result);
        }

        /// <summary>
        /// Akceptacja kosztu (tylko admin, PendingApproval → Approved)
        /// </summary>
        [HttpPost("{costId}/approve")]
        [Authorize(Policy = PermissionCodes.ProjectAdmin)]
        public async Task<IActionResult> ApproveCost(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid costId)
        {
            ApproveProjectCostCommand command = new ApproveProjectCostCommand
            {
                TenantId = tenantId,
                ProjectId = projectId,
                CostId = costId
            };
            ProjectCostListItemWeb result = await Send(command);
            return Ok(result);
        }

        /// <summary>
        /// Odrzucenie kosztu (tylko admin, PendingApproval → Draft)
        /// </summary>
        [HttpPost("{costId}/reject")]
        [Authorize(Policy = PermissionCodes.ProjectAdmin)]
        public async Task<IActionResult> RejectCost(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid costId)
        {
            RejectProjectCostCommand command = new RejectProjectCostCommand
            {
                TenantId = tenantId,
                ProjectId = projectId,
                CostId = costId
            };
            ProjectCostListItemWeb result = await Send(command);
            return Ok(result);
        }
    }
}
