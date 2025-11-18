using CQRS.Tenants.CreateTenant;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Constants;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TenantController(IMediator mediator) : BaseApiController(mediator)
    {
        [HttpPost("create")]
        [Authorize]
        public async Task<IActionResult> CreateTenant([FromBody] CreateTenantCommand request)
        {
            return Ok(await Send(request));
        }
    }
}
