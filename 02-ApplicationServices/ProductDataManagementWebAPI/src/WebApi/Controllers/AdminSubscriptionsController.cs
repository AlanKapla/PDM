using Business.Interfaces.Constants;
using Business.Interfaces.WebModels.Admin;
using Business.Interfaces.WebModels.Subscriptions;
using CQRS.Admin.Subscriptions.AddSubscriptionOverride;
using CQRS.Admin.Subscriptions.ChangeTenantPlan;
using CQRS.Admin.Subscriptions.DeactivateSubscriptionOverride;
using CQRS.Admin.Subscriptions.GetAdminPaymentHistory;
using CQRS.Admin.Subscriptions.GetAllPlanDefinitions;
using CQRS.Admin.Subscriptions.GetTenantSubscription;
using CQRS.Admin.Subscriptions.GrantFullAccess;
using CQRS.Admin.Subscriptions.RevokeFullAccess;
using CQRS.Admin.Subscriptions.UpdatePlanDefinition;
using Entities.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[Route("api/admin/subscriptions")]
[ApiController]
[Authorize(Policy = PolicyNames.Admin)]
public sealed class AdminSubscriptionsController(IMediator mediator) : BaseApiController(mediator)
{
    // ── Plans ──────────────────────────────────────────────────────────────────

    [HttpGet("plans")]
    [ProducesResponseType(typeof(IEnumerable<AdminSubscriptionPlanWeb>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetPlans()
    {
        GetAllPlanDefinitionsQuery query = new();
        IEnumerable<AdminSubscriptionPlanWeb> result = await Send(query);
        return Ok(result);
    }

    [HttpPut("plans/{plan:int}")]
    [ProducesResponseType(typeof(AdminSubscriptionPlanWeb), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePlan(
        [FromRoute] int plan,
        [FromBody] UpdatePlanDefinitionCommand command)
    {
        UpdatePlanDefinitionCommand cmd = command with { Plan = (SubscriptionPlan)plan };
        AdminSubscriptionPlanWeb result = await Send(cmd);
        return Ok(result);
    }

    // ── Tenant subscriptions ───────────────────────────────────────────────────

    [HttpGet("tenants/{tenantId:guid}")]
    [ProducesResponseType(typeof(TenantSubscriptionWeb), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTenantSubscription([FromRoute] Guid tenantId)
    {
        GetTenantSubscriptionQuery query = new(tenantId);
        TenantSubscriptionWeb result = await Send(query);
        return Ok(result);
    }

    [HttpPut("tenants/{tenantId:guid}/plan")]
    [ProducesResponseType(typeof(TenantSubscriptionSummaryWeb), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangeTenantPlan(
        [FromRoute] Guid tenantId,
        [FromBody] ChangeTenantPlanCommand command)
    {
        ChangeTenantPlanCommand cmd = command with { TenantId = tenantId };
        TenantSubscriptionSummaryWeb result = await Send(cmd);
        return Ok(result);
    }

    [HttpPost("tenants/{tenantId:guid}/full-access")]
    [ProducesResponseType(typeof(GrantFullAccessResultWeb), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GrantFullAccess([FromRoute] Guid tenantId)
    {
        GrantFullAccessCommand command = new(tenantId);
        GrantFullAccessResultWeb result = await Send(command);
        return Ok(result);
    }

    [HttpDelete("tenants/{tenantId:guid}/full-access")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeFullAccess([FromRoute] Guid tenantId)
    {
        RevokeFullAccessCommand command = new(tenantId);
        await Send(command);
        return Ok();
    }

    // ── Overrides ──────────────────────────────────────────────────────────────

    [HttpPost("tenants/{tenantId:guid}/overrides")]
    [ProducesResponseType(typeof(AddedSubscriptionOverrideWeb), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddOverride(
        [FromRoute] Guid tenantId,
        [FromBody] AddSubscriptionOverrideCommand command)
    {
        AddSubscriptionOverrideCommand cmd = command with { TenantId = tenantId };
        AddedSubscriptionOverrideWeb result = await Send(cmd);
        return Ok(result);
    }

    [HttpDelete("tenants/{tenantId:guid}/overrides/{overrideId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateOverride(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid overrideId)
    {
        DeactivateSubscriptionOverrideCommand command = new(tenantId, overrideId);
        await Send(command);
        return Ok();
    }

    // ── Payment history ────────────────────────────────────────────────────────

    [HttpGet("tenants/{tenantId:guid}/payments")]
    [ProducesResponseType(typeof(IEnumerable<SubscriptionPaymentWeb>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPaymentHistory([FromRoute] Guid tenantId)
    {
        GetAdminPaymentHistoryQuery query = new(tenantId);
        IEnumerable<SubscriptionPaymentWeb> result = await Send(query);
        return Ok(result);
    }
}
