using Entities.Models.Subscriptions;
using FluentValidation;

namespace CQRS.Admin.Subscriptions.AddSubscriptionOverride;

public sealed class AddSubscriptionOverrideCommandValidator : AbstractValidator<AddSubscriptionOverrideCommand>
{
    public AddSubscriptionOverrideCommandValidator()
    {
        RuleFor(x => x.Key)
            .NotEmpty()
            .MaximumLength(256);

        RuleFor(x => x.Value)
            .NotEmpty()
            .MaximumLength(256);

        RuleFor(x => x.Reason)
            .NotEmpty()
            .MaximumLength(1024);

        RuleFor(x => x.Value)
            .Must(value => int.TryParse(value, out int parsed) && parsed >= -1)
            .WithMessage("Value must be a valid integer >= -1 for MaxProjects/MaxUsers overrides.")
            .When(x => x.Key == SubscriptionOverride.Keys.MaxProjects
                     || x.Key == SubscriptionOverride.Keys.MaxUsers);
    }
}
