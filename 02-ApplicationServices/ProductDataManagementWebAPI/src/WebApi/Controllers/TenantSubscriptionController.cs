using Business.Interfaces.Constants;
using Business.Interfaces.WebModels.Subscriptions;
using CQRS.Subscriptions.GetMyTenantSubscription;
using CQRS.Subscriptions.GetPaymentHistory;
using CQRS.Subscriptions.GetPublicSubscriptionPlans;
using CQRS.Subscriptions.GetSubscriptionStatus;
using CQRS.Subscriptions.ProcessMockPayment;
using CQRS.Subscriptions.RequestPlanChange;
using Entities.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[Route("api/tenants/{tenantId:guid}/subscription")]
[ApiController]
[Authorize]
public sealed class TenantSubscriptionController(IMediator mediator) : BaseApiController(mediator)
{
    // ── Moja subskrypcja ────────────────────────────────────────────────────────

    [HttpGet]
    [Authorize(Policy = PermissionCodes.TenantView)]
    [ProducesResponseType(typeof(TenantSubscriptionInfoWeb), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMySubscription(
        [FromRoute] Guid tenantId)
    {
        GetMyTenantSubscriptionQuery query = new(tenantId);
        TenantSubscriptionInfoWeb result = await Send(query);
        return Ok(result);
    }

    // ── Dostępne plany ──────────────────────────────────────────────────────────

    [HttpGet("plans")]
    [Authorize(Policy = PermissionCodes.TenantView)]
    [ProducesResponseType(typeof(IEnumerable<SubscriptionPlanInfoWeb>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAvailablePlans(
        [FromRoute] Guid tenantId)
    {
        GetPublicSubscriptionPlansQuery query = new(tenantId);
        IEnumerable<SubscriptionPlanInfoWeb> result = await Send(query);
        return Ok(result);
    }

    // ── Zmiana planu ────────────────────────────────────────────────────────────

    [HttpPut("plan")]
    [Authorize(Policy = PermissionCodes.TenantEdit)]
    [ProducesResponseType(typeof(TenantSubscriptionInfoWeb), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RequestPlanChange(
        [FromRoute] Guid tenantId,
        [FromBody] RequestPlanChangeCommand command)
    {
        command = command with { TenantId = tenantId };
        TenantSubscriptionInfoWeb result = await Send(command);
        return Ok(result);
    }

    // ── Mock płatność ───────────────────────────────────────────────────────────

    [HttpPost("pay")]
    [Authorize(Policy = PermissionCodes.TenantEdit)]
    [ProducesResponseType(typeof(MockPaymentResultWeb), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ProcessMockPayment(
        [FromRoute] Guid tenantId)
    {
        ProcessMockPaymentCommand command = new(tenantId);
        MockPaymentResultWeb result = await Send(command);
        return Ok(result);
    }

    // ── Status billingu ─────────────────────────────────────────────────────────

    [HttpGet("status")]
    [Authorize(Policy = PermissionCodes.TenantView)]
    [ProducesResponseType(typeof(SubscriptionStatusWeb), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSubscriptionStatus(
        [FromRoute] Guid tenantId)
    {
        GetSubscriptionStatusQuery query = new(tenantId);
        SubscriptionStatusWeb result = await Send(query);
        return Ok(result);
    }

    // ── Historia płatności ──────────────────────────────────────────────────────

    [HttpGet("payments")]
    [Authorize(Policy = PermissionCodes.TenantView)]
    [ProducesResponseType(typeof(IEnumerable<SubscriptionPaymentWeb>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPaymentHistory(
        [FromRoute] Guid tenantId)
    {
        GetPaymentHistoryQuery query = new(tenantId);
        IEnumerable<SubscriptionPaymentWeb> result = await Send(query);
        return Ok(result);
    }
}
