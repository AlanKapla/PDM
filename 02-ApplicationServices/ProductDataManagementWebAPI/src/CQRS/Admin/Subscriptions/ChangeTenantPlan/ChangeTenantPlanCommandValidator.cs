using Entities.Enums;
using FluentValidation;

namespace CQRS.Admin.Subscriptions.ChangeTenantPlan;

public sealed class ChangeTenantPlanCommandValidator : AbstractValidator<ChangeTenantPlanCommand>
{
    public ChangeTenantPlanCommandValidator()
    {
        RuleFor(x => x.Plan)
            .Must(p => Enum.IsDefined(typeof(SubscriptionPlan), p))
            .WithMessage("'{PropertyName}' must be a valid SubscriptionPlan value.");
    }
}
