using FluentValidation;

namespace CQRS.Users.UserRefresh
{
    public class UserRefreshQueryValidator : AbstractValidator<UserRefreshQuery>
    {
        public UserRefreshQueryValidator()
        {
            RuleFor(x => x.RefreshToken)
                .NotEmpty().WithMessage("RefreshToken can not be empty")
                .MaximumLength(200).WithMessage("RefreshToken is too long");
        }
    }
}
