using Business.Interfaces.WebModels.Users;

namespace CQRS.Users.UserPasswordResetRequest
{
    public sealed record UserPasswordResetRequestCommand(string Email) : IRequestCommand<UserPasswordResetRequestWeb>
    {
    }
}
