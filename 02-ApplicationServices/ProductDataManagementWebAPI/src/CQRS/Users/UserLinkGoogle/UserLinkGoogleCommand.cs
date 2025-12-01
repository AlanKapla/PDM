using Business.Interfaces.Model;

namespace CQRS.Users.UserLinkGoogle
{
    public sealed record UserLinkGoogleCommand(
        string GoogleToken
    ) : IRequestCommand<UserAuthWeb>;
}