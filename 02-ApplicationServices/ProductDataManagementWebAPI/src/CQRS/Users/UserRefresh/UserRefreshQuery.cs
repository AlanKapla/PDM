namespace CQRS.Users.UserRefresh
{
    public sealed record UserRefreshQuery : IRequestQuery<UserAuthWeb>
    {
        public string RefreshToken { get; set; } = string.Empty;
    }
}