using Business.Interfaces.Services;
using Business.Interfaces.WebModels.Users;
using CQRS.Users.UserDetails;
using CQRS.Users.UserLogin;
using CQRS.Users.UserLogout;
using CQRS.Users.UserRefresh;
using CQRS.Users.UserRegister;
using CQRS.Users.UserResetPassword;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController : BaseApiController
{
    private readonly IHttpCookieService cookieService;

    public UserController(IMediator mediator, IHttpCookieService cookieService) : base(mediator)
    {
        this.cookieService = cookieService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] UserRegisterCommand request)
    {
        return Ok(await Send(request));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] UserLoginCommand request)
    {
        UserAuthWeb userAuthWeb = await Send(request);

        cookieService.SetAccessToken(userAuthWeb.AccessToken, userAuthWeb.AccessTokenExpiresAt);
        cookieService.SetRefreshToken(userAuthWeb.RefreshToken, userAuthWeb.RefreshTokenExpiresAt);

        return Ok();
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] UserRefreshQuery request)
    {
        UserAuthWeb userAuthWeb = await Send(request);
        
        cookieService.SetAccessToken(userAuthWeb.AccessToken, userAuthWeb.AccessTokenExpiresAt);
        cookieService.SetRefreshToken(userAuthWeb.RefreshToken, userAuthWeb.RefreshTokenExpiresAt);

        return Ok();
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] UserLogoutCommand request)
    {
        UserLogoutWeb userLogoutWeb = await Send(request);

        cookieService.ClearAuthCookies();

        return Ok(userLogoutWeb);
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] UserResetPasswordCommand request)
    {
        return Ok(await Send(request));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetUserDetails([FromQuery] UserDetailsQuery request)
    {
        return Ok(await Send(request));
    }
}
