using Business.Interfaces.Constants;
using Business.Interfaces.WebModels.Admin;
using CQRS.Admin.Tenants.GetAdminTenantDetails;
using CQRS.Admin.Tenants.GetAdminTenants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[Route("api/admin/tenants")]
[ApiController]
[Authorize(Policy = PolicyNames.Admin)]
public sealed class AdminTenantsController(IMediator mediator) : BaseApiController(mediator)
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<AdminTenantListItemWeb>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetTenants()
    {
        GetAdminTenantsQuery query = new();
        IEnumerable<AdminTenantListItemWeb> result = await Send(query);
        return Ok(result);
    }

    [HttpGet("{tenantId:guid}")]
    [ProducesResponseType(typeof(AdminTenantDetailsWeb), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTenantDetails([FromRoute] Guid tenantId)
    {
        GetAdminTenantDetailsQuery query = new(tenantId);
        AdminTenantDetailsWeb result = await Send(query);
        return Ok(result);
    }
}
