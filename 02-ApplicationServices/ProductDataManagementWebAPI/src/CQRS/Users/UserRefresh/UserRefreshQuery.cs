namespace CQRS.Users.UserRefresh
{
    public sealed record UserRefreshQuery(string RefreshToken) : IRequestQuery<UserAuthWeb>;
}