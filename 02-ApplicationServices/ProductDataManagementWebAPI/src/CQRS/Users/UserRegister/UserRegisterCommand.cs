using Business.Interfaces.WebModels.Users;
using MediatR;

namespace CQRS.Users.UserRegister
{
    public sealed record UserRegisterCommand(
        string Email,
        string Password,
        string FirstName,
        string LastName
    ) : IRequestCommand<UserRegisterWeb>;
}