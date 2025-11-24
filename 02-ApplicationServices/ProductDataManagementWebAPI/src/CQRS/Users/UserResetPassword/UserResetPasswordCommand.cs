using Business.Interfaces.WebModels.Users;

namespace CQRS.Users.UserResetPassword
{
    public sealed record UserResetPasswordCommand(string Token, string Password) : IRequestCommand<UserResetPasswordWeb>
    {
    }
}