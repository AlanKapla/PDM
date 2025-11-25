using CQRS.Tenants.ChangeActiveTenant;
using CQRS.Tenants.CreateTenant;
using CQRS.Tenants.UserTenants;
using CQRS.Tenants.ActiveTenant;
using CQRS.Tenants.UpdateTenant;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CQRS.Tenants.InviteTenantMember;
using CQRS.Tenants.AcceptTenantInvitation;
using CQRS.Tenants.RemoveTenantMember;
using CQRS.Tenants.ActiveInvitations;

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
            object result = await Send(request);
            return Ok(result);
        }

        [HttpGet("user-tenants")]
        [Authorize]
        public async Task<IActionResult> GetUserTenants()
        {
            object result = await Send(new UserTenantsQuery());
            return Ok(result);
        }

        [HttpPut("active")]
        [Authorize]
        public async Task<IActionResult> ChangeActiveTenant([FromBody] ChangeActiveTenantCommand request)
        {
            object result = await Send(request);
            return Ok(result);
        }

        [HttpGet("active")]
        [Authorize]
        public async Task<IActionResult> GetActiveTenant()
        {
            object result = await Send(new ActiveTenantQuery());
            return Ok(result);
        }

        [HttpPut("{tenantId}")]
        [Authorize(Policy = "TenantAdmin")]
        public async Task<IActionResult> UpdateTenant(Guid tenantId, [FromBody] UpdateTenantCommand body)
        {
            if (tenantId != body.TenantId)
            {
                return BadRequest("Route tenantId differs from body TenantId.");
            }
            object result = await Send(body);
            return Ok(result);
        }

        [HttpPost("{tenantId}/invitations")]
        [Authorize(Policy = "TenantAdmin")]
        public async Task<IActionResult> InviteTenantMember(Guid tenantId, [FromBody] InviteTenantMemberCommand body)
        {
            if (tenantId != body.TenantId)
            {
                return BadRequest("Route tenantId differs from body TenantId.");
            }
            await Send(body);
            return Ok();
        }

        [HttpGet("invitations")]
        [Authorize]
        public async Task<IActionResult> GetActiveInvitations()
        {
            object result = await Send(new ActiveTenantInvitationsQuery());
            return Ok(result);
        }

        [HttpPost("invitations/accept")]
        [Authorize]
        public async Task<IActionResult> AcceptInvitation([FromBody] AcceptTenantInvitationCommand body)
        {
            await Send(body);
            return Ok();
        }

        [HttpDelete("{tenantId}/members/{userId}")]
        [Authorize(Policy = "TenantAdmin")]
        public async Task<IActionResult> RemoveTenantMember(Guid tenantId, Guid userId)
        {
            await Send(new RemoveTenantMemberCommand(tenantId, userId));
            return NoContent();
        }
    }
}
