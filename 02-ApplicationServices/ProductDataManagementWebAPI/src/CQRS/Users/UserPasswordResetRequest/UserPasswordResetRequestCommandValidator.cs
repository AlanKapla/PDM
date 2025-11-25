using FluentValidation;

namespace CQRS.Users.UserPasswordResetRequest
{
    public class UserPasswordResetRequestCommandValidator : AbstractValidator<UserPasswordResetRequestCommand>
    {
        public UserPasswordResetRequestCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("Invalid email format");
        }
    }
}
