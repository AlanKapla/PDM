using FluentValidation;

namespace CQRS.Users.UserGoogleRegister
{
    public class UserGoogleRegisterCommandValidator : AbstractValidator<UserGoogleRegisterCommand>
    {
        public UserGoogleRegisterCommandValidator()
        {
            RuleFor(x => x.GoogleToken)
                .NotEmpty()
                .WithMessage("Google token is required");
        }
    }
}