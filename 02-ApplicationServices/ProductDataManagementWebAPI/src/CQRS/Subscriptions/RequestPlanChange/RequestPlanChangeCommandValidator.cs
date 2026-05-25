using Entities.Enums;
using FluentValidation;

namespace CQRS.Subscriptions.RequestPlanChange;

public sealed class RequestPlanChangeCommandValidator : AbstractValidator<RequestPlanChangeCommand>
{
    public RequestPlanChangeCommandValidator()
    {
        RuleFor(x => x.Plan)
            .Must(p => Enum.IsDefined(typeof(SubscriptionPlan), p))
            .WithMessage("'{PropertyName}' must be a valid SubscriptionPlan value.");
    }
}
