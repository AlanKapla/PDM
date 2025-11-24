using Business.Interfaces.WebModels.Users;

namespace CQRS.Users.UserActivate
{
    public sealed record UserActivateCommand(string Token) : IRequestCommand<UserActivateWeb>
    {
    }
}
