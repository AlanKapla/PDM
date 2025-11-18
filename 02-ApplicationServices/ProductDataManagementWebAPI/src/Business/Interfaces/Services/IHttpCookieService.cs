namespace Business.Interfaces.Services
{
    public interface IHttpCookieService
    {
        void ClearAuthCookies();
        string? GetRefreshToken();
        void SetAccessToken(string accessToken, DateTime expiresAt);
        void SetRefreshToken(string? refreshToken, DateTime? expiresAt);
    }
}