namespace CQRS.Users.UserLogin
{
    public sealed record UserLoginCommand(
        string Email,
        string Password,
        string ExternalToken,
        LoginProvider Provider = LoginProvider.Local
    ) : IRequestCommand<UserAuthWeb>;

    public enum LoginProvider
    {
        Local,
        Google
    }
}
