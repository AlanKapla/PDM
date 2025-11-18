using Business.Interfaces.Constants;
using Business.Interfaces.Services;
using Microsoft.AspNetCore.Http;

namespace Business.Implementation.Services
{
    public class HttpCookieService : IHttpCookieService
    {
        private readonly IHttpContextAccessor contextAccessor;

        public HttpCookieService(IHttpContextAccessor contextAccessor)
        {
            this.contextAccessor = contextAccessor;
        }

        public string? GetRefreshToken()
        {
            string? refreshToken = contextAccessor.HttpContext?.Request.Cookies[CookieKeys.RefreshToken];

            return refreshToken;
        }

        public void SetAccessToken(string accessToken, DateTime expiresAt)
        {
            contextAccessor.HttpContext?.Response.Cookies.Append(CookieKeys.AccessToken, accessToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = expiresAt,
            });
        }

        public void SetRefreshToken(string? refreshToken, DateTime? expiresAt)
        {
            if (refreshToken == null)
            {
                return;
            }

            contextAccessor.HttpContext?.Response.Cookies.Append(CookieKeys.RefreshToken, refreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = expiresAt,
            });
        }

        public void ClearAuthCookies()
        {
            contextAccessor.HttpContext?.Response.Cookies.Append(
                CookieKeys.AccessToken, "",
                new CookieOptions
                {
                    Expires = DateTime.UtcNow.AddDays(-1),
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict
                });

            contextAccessor.HttpContext?.Response.Cookies.Append(
                CookieKeys.RefreshToken, "",
                new CookieOptions
                {
                    Expires = DateTime.UtcNow.AddDays(-1),
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict
                });
        }
    }
}
