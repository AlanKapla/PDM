using FluentValidation;

namespace CQRS.Admin.ColdMails.GetColdMailHistory
{
    public sealed class GetColdMailHistoryQueryValidator : AbstractValidator<GetColdMailHistoryQuery>
    {
        public const int MaxEmailFilterLength = 320;

        public GetColdMailHistoryQueryValidator()
        {
            RuleFor(x => x.Email)
                .MaximumLength(MaxEmailFilterLength)
                .WithMessage($"'Email' must not exceed {MaxEmailFilterLength} characters.")
                .When(x => x.Email is not null);
        }
    }
}
