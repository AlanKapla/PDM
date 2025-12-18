using Business.Interfaces.Model;

namespace CQRS.Users.UserGoogleRegister
{
    public sealed record UserGoogleRegisterCommand(
        string GoogleToken
    ) : IRequestCommand<UserAuthWeb>;
}