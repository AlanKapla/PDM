using Business.Interfaces.Constants;
using Business.Interfaces.WebModels.ProjectDashboard;
using CQRS.ProjectDashboard.GetProjectDashboard;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    /// <summary>
    /// Controller for the project dashboard — aggregated financial and timeline data.
    /// </summary>
    [ApiController]
    [Route("api/tenants/{tenantId:guid}/projects/{projectId:guid}/dashboard")]
    public class ProjectDashboardController : BaseApiController
    {
        public ProjectDashboardController(IMediator mediator) : base(mediator)
        {
        }

        /// <summary>
        /// Get full cost tracker details for a project (all estimates aggregated)
        /// </summary>
        /// <param name="tenantId">Tenant ID</param>
        /// <param name="projectId">Project ID</param>
        /// <returns>Cost tracker details with project summary, per-estimate summaries and project-level additional costs</returns>
        [HttpGet]
        [Authorize(Policy = PermissionCodes.ProjectEdit)]
        [ProducesResponseType(typeof(ProjectDashboardWeb), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetProjectDashboard(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId)
        {
            GetProjectDashboardQuery query = new GetProjectDashboardQuery
            {
                TenantId = tenantId,
                ProjectId = projectId
            };

            return Ok(await Send(query));
        }
    }
}
