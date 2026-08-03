using CQRS.Users.UserAuthStatus;
using CQRS.Users.UserDetails;
using CQRS.Users.UserSyncFromB2C;
using CQRS.Users.UserUpdate;
using CQRS.WorkSchedules.GetUserAssignedWorks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[ApiController]
[Route("api/user")]
public class UserController : BaseApiController
{
    public UserController(IMediator mediator) : base(mediator)
    {
    }

    /// <summary>
    /// Get current user authentication status
    /// </summary>
    [Authorize]
    [HttpGet("auth-status")]
    public async Task<IActionResult> GetAuthStatus()
    {
        UserAuthStatusQuery request = new();
        return Ok(await Send(request));
    }

    /// <summary>
    /// Syncs Azure AD B2C user to local database. Called automatically on first login.
    /// Extracts user information from Azure AD B2C token claims.
    /// </summary>
    [Authorize]
    [HttpPost("sync-b2c")]
    public async Task<IActionResult> SyncFromB2C()
    {
        UserSyncFromB2CCommand command = new();
        Guid userId = await Send(command);

        return Ok(new { userId, message = "User synced successfully" });
    }

    /// <summary>
    /// Update current user profile
    /// </summary>
    [Authorize]
    [HttpPut("me")]
    public async Task<IActionResult> UpdateMe([FromBody] UserUpdateCommand request)
    {
        return Ok(await Send(request));
    }

    /// <summary>
    /// Get current user details
    /// </summary>
    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetUserDetails()
    {
        UserDetailsQuery request = new();
        return Ok(await Send(request));
    }

    /// <summary>
    /// Gets all works assigned to the current user across all tenants
    /// Grouped by Project > WorkSchedule > Stage > Work with period information
    /// </summary>
    /// <returns>Hierarchically grouped assigned works with periods</returns>
    [Authorize]
    [HttpGet("assigned-works")]
    public async Task<IActionResult> GetMyAssignedWorks()
    {
        GetUserAssignedWorksQuery query = new GetUserAssignedWorksQuery();
        return Ok(await Send(query));
    }

}
