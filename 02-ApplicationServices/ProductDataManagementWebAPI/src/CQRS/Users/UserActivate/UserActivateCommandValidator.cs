using FluentValidation;

namespace CQRS.Users.UserActivate
{
    public class UserActivateCommandValidator : AbstractValidator<UserActivateCommand>
    {
        public UserActivateCommandValidator()
        {
            RuleFor(x => x.Token)
                .NotEmpty().WithMessage("Token is required");
        }
    }
}
