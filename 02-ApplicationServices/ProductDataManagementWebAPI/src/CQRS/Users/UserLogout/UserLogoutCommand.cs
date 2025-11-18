using Business.Interfaces.WebModels.Users;
using MediatR;

namespace CQRS.Users.UserLogout
{
    public sealed record UserLogoutCommand(string RefreshToken) : IRequestCommand<UserLogoutWeb>;
}