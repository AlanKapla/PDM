using CQRS.Users.UserResetPassword;
using FluentValidation;

namespace CQRS.Users.UserRegister;

public class UserResetPasswordCommandValidator : AbstractValidator<UserResetPasswordCommand>
{
    public UserResetPasswordCommandValidator()
    {
        RuleFor(x => x.Password)
        .NotNull().WithMessage("Password is required")
        .NotEmpty().WithMessage("Password is required")
        .MinimumLength(8).WithMessage("Password must be at least 8 characters long")
        .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter")
        .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter")
        .Matches("[0-9]").WithMessage("Password must contain at least one digit")
        .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character");

        RuleFor(x => x.Email)
            .NotNull().WithMessage("Email is required")
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");
    }
}