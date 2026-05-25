using Business.Interfaces.Constants;
using Business.Interfaces.WebModels.Admin;
using CQRS.Admin.Subscriptions.GetSubscriptionPlans;
using CQRS.Admin.Subscriptions.UpdateSubscriptionPlan;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[Route("api/admin/subscription-plans")]
[ApiController]
[Authorize(Policy = PolicyNames.Admin)]
public sealed class AdminSubscriptionPlansController(IMediator mediator) : BaseApiController(mediator)
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<SubscriptionPlanDefinitionWeb>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetPlans()
    {
        GetSubscriptionPlansQuery query = new();
        IEnumerable<SubscriptionPlanDefinitionWeb> result = await Send(query);
        return Ok(result);
    }

    [HttpPut("{planId:guid}")]
    [ProducesResponseType(typeof(SubscriptionPlanDefinitionWeb), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePlan([FromRoute] Guid planId, [FromBody] UpdateSubscriptionPlanCommand command)
    {
        UpdateSubscriptionPlanCommand cmd = command with { Id = planId };
        SubscriptionPlanDefinitionWeb result = await Send(cmd);
        return Ok(result);
    }
}
