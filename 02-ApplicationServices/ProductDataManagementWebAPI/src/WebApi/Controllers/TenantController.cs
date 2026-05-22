using Business.Interfaces.Constants;
using Business.Interfaces.WebModels.Tenants;
using CQRS.Tenants.AcceptTenantInvitation;
using CQRS.Tenants.ActiveInvitations;
using CQRS.Tenants.ChangeActiveTenant;
using CQRS.Tenants.CreateTenant;
using CQRS.Tenants.RemoveTenantInvitation;
using CQRS.Tenants.GetTenantMembers;
using CQRS.Tenants.InviteTenantMember;
using CQRS.Tenants.RemoveTenantMember;
using CQRS.Tenants.ToggleTenantStatus;
using CQRS.Tenants.UpdateTenant;
using CQRS.Tenants.GetUserTenants;
using CQRS.Tenants.GetAdminTenants;
using CQRS.Tenants.GetTenantDetails;
using CQRS.Tenants.UpdateTenantMemberRole;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [Route("api/tenants")]
    [ApiController]
    public class TenantController(IMediator mediator) : BaseApiController(mediator)
    {
        [HttpPost("create")]
        [Authorize]
        [ProducesResponseType(typeof(TenantDetailsWeb), StatusCodes.Status201Created)]
        public async Task<IActionResult> CreateTenant([FromBody] CreateTenantCommand request)
        {
            TenantDetailsWeb result = await Send(request);
            return CreatedAtAction(nameof(GetTenantDetails), new { tenantId = result.Id }, result);
        }

        /// <summary>
        /// Get all tenants where current user is a member (admins see inactive, members see only active)
        /// </summary>
        [HttpGet("my-tenants")]
        [Authorize(Policy = PermissionCodes.TenantListAvailable)]
        public async Task<IActionResult> GetMyTenants()
        {
            IEnumerable<UserTenantWeb> result = await Send(new GetUserTenantsQuery());
            return Ok(result);
        }

        /// <summary>
        /// Get all tenants where current user is admin (includes active and inactive)
        /// </summary>
        [HttpGet("admin-tenants")]
        [Authorize(Policy = PermissionCodes.TenantAdminListAvailable)]
        public async Task<IActionResult> GetAdminTenants()
        {
            IEnumerable<TenantBasicWeb> result = await Send(new GetAdminTenantsQuery());
            return Ok(result);
        }

        /// <summary>
        /// Get detailed tenant information including members and invitations (admin only)
        /// </summary>
        [HttpGet("{tenantId}/details")]
        [Authorize(Policy = PermissionCodes.TenantEdit)]
        public async Task<IActionResult> GetTenantDetails(Guid tenantId)
        {
            GetTenantDetailsQuery query = new GetTenantDetailsQuery { TenantId = tenantId };
            TenantDetailsWeb result = await Send(query);
            return Ok(result);
        }

        [HttpPut("active")]
        [Authorize]
        public async Task<IActionResult> ChangeActiveTenant([FromBody] ChangeActiveTenantCommand request)
        {
            ActiveTenantWeb result = await Send(request);
            return Ok(result);
        }

        [HttpPut("{tenantId}")]
        [Authorize(Policy = PermissionCodes.TenantEdit)]
        public async Task<IActionResult> UpdateTenant(Guid tenantId, [FromBody] UpdateTenantCommand request)
        {
            request = request with { TenantId = tenantId };
            TenantDetailsWeb result = await Send(request);
            return Ok(result);
        }

        [HttpPost("{tenantId}/invitations")]
        [Authorize(Policy = PermissionCodes.TenantMembersManage)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> InviteTenantMember(Guid tenantId, [FromBody] InviteTenantMemberCommand request)
        {
            request = request with { TenantId = tenantId };
            await Send(request);
            return NoContent();
        }

        [HttpGet("invitations")]
        [Authorize]
        public async Task<IActionResult> GetActiveInvitations()
        {
            IEnumerable<TenantInvitationWeb> result = await Send(new ActiveTenantInvitationsQuery());
            return Ok(result);
        }

        [HttpPost("invitations/accept")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> AcceptInvitation([FromBody] AcceptTenantInvitationCommand request)
        {
            await Send(request);
            return NoContent();
        }

        [HttpDelete("{tenantId}/invitations/{invitationId}")]
        [Authorize(Policy = PermissionCodes.TenantMembersManage)]
        public async Task<IActionResult> RemoveInvitation(Guid tenantId, Guid invitationId)
        {
            RemoveTenantInvitationCommand command = new RemoveTenantInvitationCommand { TenantId = tenantId, InvitationId = invitationId };
            await Send(command);
            return NoContent();
        }

        [HttpGet("{tenantId}/members")]
        [Authorize(Policy = PermissionCodes.TenantView)]
        public async Task<IActionResult> GetTenantMembers(Guid tenantId)
        {
            GetTenantMembersQuery query = new GetTenantMembersQuery { TenantId = tenantId };
            IEnumerable<TenantMemberWeb> result = await Send(query);
            return Ok(result);
        }

        [HttpDelete("{tenantId}/members/{userId}")]
        [Authorize(Policy = PermissionCodes.TenantMembersManage)]
        public async Task<IActionResult> RemoveTenantMember(Guid tenantId, Guid userId)
        {
            await Send(new RemoveTenantMemberCommand { TenantId = tenantId, UserId = userId });
            return NoContent();
        }

        [HttpPatch("{tenantId}/status")]
        [Authorize(Policy = PermissionCodes.TenantStatusManage)]
        public async Task<IActionResult> ToggleTenantStatus([FromRoute] Guid tenantId, [FromQuery] bool isActive)
        {
            ToggleTenantStatusCommand command = new ToggleTenantStatusCommand { TenantId = tenantId, IsActive = isActive };
            await Send(command);
            return NoContent();
        }

        [HttpPatch("{tenantId}/members/{userId}/role")]
        [Authorize(Policy = PermissionCodes.TenantMembersManage)]
        public async Task<IActionResult> UpdateTenantMemberRole(
            Guid tenantId,
            Guid userId,
            [FromBody] UpdateTenantMemberRoleCommand request)
        {
            request = request with { TenantId = tenantId, UserId = userId };
            await Send(request);
            return NoContent();
        }
    }
}
