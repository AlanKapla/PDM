using FluentValidation;

namespace CQRS.Activity.RecordDemoActivity
{
    public sealed class RecordDemoActivityCommandValidator
        : AbstractValidator<RecordDemoActivityCommand>
    {
        public const int MaxRouteLength = 500;
        public const int MaxIpAddressLength = 45;

        public RecordDemoActivityCommandValidator()
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
