using Entities.Enums;
using FluentValidation;

namespace CQRS.Admin.Subscriptions.UpdatePlanDefinition;

public sealed class UpdatePlanDefinitionCommandValidator : AbstractValidator<UpdatePlanDefinitionCommand>
{
    public UpdatePlanDefinitionCommandValidator()
    {
        RuleFor(x => x.Plan)
            .Must(p => Enum.IsDefined(typeof(SubscriptionPlan), p))
            .WithMessage("'{PropertyName}' must be a valid SubscriptionPlan value.");

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(128);

        RuleFor(x => x.MaxProjects)
            .GreaterThanOrEqualTo(-1);

        RuleFor(x => x.MaxUsers)
            .GreaterThanOrEqualTo(-1);

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0m);

        RuleFor(x => x.Currency)
            .NotEmpty()
            .MaximumLength(8);
    }
}
