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

            RuleFor(x => x.PhoneNumber)
                .MaximumLength(20).WithMessage("Phone number cannot exceed 20 characters")
                .When(x => x.PhoneNumber is not null);

            RuleFor(x => x.CompanyName)
                .MaximumLength(200).WithMessage("Company name cannot exceed 200 characters")
                .When(x => x.CompanyName is not null);

            RuleFor(x => x.TaxId)
                .MaximumLength(50).WithMessage("Tax ID cannot exceed 50 characters")
                .When(x => x.TaxId is not null);

            RuleFor(x => x.Street)
                .MaximumLength(200).WithMessage("Street cannot exceed 200 characters")
                .When(x => x.Street is not null);

            RuleFor(x => x.City)
                .MaximumLength(100).WithMessage("City cannot exceed 100 characters")
                .When(x => x.City is not null);

            RuleFor(x => x.PostalCode)
                .MaximumLength(20).WithMessage("Postal code cannot exceed 20 characters")
                .When(x => x.PostalCode is not null);

            RuleFor(x => x.Country)
                .MaximumLength(100).WithMessage("Country cannot exceed 100 characters")
                .When(x => x.Country is not null);
        }
    }
}