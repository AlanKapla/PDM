using FluentValidation;
using Business.Interfaces.Model;

namespace CQRS.Users.UserUpdate
{
    public class UserUpdateCommandValidator : AbstractValidator<UserUpdateCommand>
    {
        public UserUpdateCommandValidator(ICurrentUser currentUser)
        {
            RuleFor(_ => currentUser.IsAuthenticated)
                .Equal(true)
                .WithMessage("User must be authenticated.");

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
}