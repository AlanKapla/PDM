using FluentValidation;

namespace CQRS.Users.UserLogin
{
    public class UserLoginCommandValidator : AbstractValidator<UserLoginCommand>
    {
        public UserLoginCommandValidator()
        {
            RuleFor(x => x.Provider)
                .IsInEnum();

            When(x => x.Provider == LoginProvider.Local, () =>
            {
                RuleFor(x => x.Email)
                    .NotEmpty().WithMessage("Email is required")
                    .EmailAddress().WithMessage("Invalid email format");

                RuleFor(x => x.Password)
                    .NotEmpty().WithMessage("Password is required")
                    .MinimumLength(8).WithMessage("Password must be at least 8 characters long");
            });

            When(x => x.Provider == LoginProvider.Google, () =>
            {
                RuleFor(x => x.ExternalToken)
                    .NotEmpty().WithMessage("External token is required for Google login");
            });
        }
    }
}
