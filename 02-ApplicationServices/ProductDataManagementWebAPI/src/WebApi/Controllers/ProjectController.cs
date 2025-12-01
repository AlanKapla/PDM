using CQRS.Projects.CreateProject;
using CQRS.Projects.GetTenantProjects;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Constants;

namespace WebApi.Controllers
{
    [Route("api/tenants/{tenantId}/[controller]")]
    [ApiController]
    [Authorize(Policy = Policies.TenantMember)]
    public class ProjectController(IMediator mediator) : BaseApiController(mediator)
    {
        [HttpGet]
        public async Task<IActionResult> GetTenantProjects([FromRoute] Guid tenantId)
        {
            var query = new GetTenantProjectsQuery(tenantId);
            var result = await Send(query);
            return Ok(result);
        }

        [HttpPost]
        [Authorize(Policy = Policies.TenantAdmin)]
        public async Task<IActionResult> CreateProject([FromRoute] Guid tenantId, [FromBody] CreateProjectCommand command)
        {
            var result = await Send(command);
            return CreatedAtAction(nameof(GetTenantProjects), new { tenantId }, result);
        }
    }
}