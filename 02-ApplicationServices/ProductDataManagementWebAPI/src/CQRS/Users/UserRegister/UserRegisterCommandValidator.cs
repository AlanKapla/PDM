using FluentValidation;

namespace CQRS.Users.UserRegister;

public class UserRegisterCommandValidator : AbstractValidator<UserRegisterCommand>
{
    public UserRegisterCommandValidator()
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

        RuleFor(x => x.FirstName)
            .NotNull().WithMessage("First name is required")
            .NotEmpty().WithMessage("First name is required")
            .MaximumLength(100).WithMessage("First name cannot exceed 100 characters");

        RuleFor(x => x.LastName)
            .NotNull().WithMessage("Last name is required")
            .NotEmpty().WithMessage("Last name is required")
            .MaximumLength(100).WithMessage("Last name cannot exceed 100 characters");
    }
}