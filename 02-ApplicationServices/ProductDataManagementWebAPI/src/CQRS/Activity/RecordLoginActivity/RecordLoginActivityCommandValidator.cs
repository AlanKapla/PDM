using FluentValidation;

namespace CQRS.Activity.RecordLoginActivity
{
    public sealed class RecordLoginActivityCommandValidator
        : AbstractValidator<RecordLoginActivityCommand>
    {
        public const int MaxRouteLength = 500;
        public const int MaxIpAddressLength = 45;

        public RecordLoginActivityCommandValidator()
        {
            RuleFor(x => x.IpAddress)
                .NotEmpty().WithMessage("'IpAddress' is required.")
                .MaximumLength(MaxIpAddressLength)
                .WithMessage($"'IpAddress' must not exceed {MaxIpAddressLength} characters.");

            RuleFor(x => x.Route)
                .MaximumLength(MaxRouteLength)
                .WithMessage($"'Route' must not exceed {MaxRouteLength} characters.")
                .When(x => x.Route is not null);
        }
    }
}
