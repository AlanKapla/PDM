using Business.Interfaces.Model;

namespace CQRS.Users.UserAuthStatus
{
    public sealed record UserAuthStatusQuery() : IRequestQuery<UserAuthStatusWeb>;

    public sealed record UserAuthStatusWeb(
        bool HasLocalAuth,
        bool HasGoogleAuth,
        bool IsHybridAuth
    );
}