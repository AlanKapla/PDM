using FluentValidation;

namespace CQRS.Users.UserLinkGoogle
{
    public class UserLinkGoogleCommandValidator : AbstractValidator<UserLinkGoogleCommand>
    {
        public UserLinkGoogleCommandValidator()
        {
            RuleFor(x => x.GoogleToken)
                .NotEmpty()
                .WithMessage("Google token is required");
        }
    }
}