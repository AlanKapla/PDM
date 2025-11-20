using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Entities.Enums;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Business.Implementation.Model
{
    public class CurrentUser : ICurrentUser
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUser(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        private ClaimsPrincipal HttpUser =>
            _httpContextAccessor.HttpContext?.User
            ?? new ClaimsPrincipal(new ClaimsIdentity());

        public Guid Id =>
            Guid.TryParse(HttpUser.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var value)
                ? value
                : Guid.Empty;

        public string FirstName => HttpUser.FindFirst(ClaimNames.FirstName)?.Value ?? string.Empty;

        public string LastName => HttpUser.FindFirst(ClaimNames.LastName)?.Value ?? string.Empty;

        public string Email =>
            HttpUser.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;

        public Guid? ActiveTenantId =>
            Guid.TryParse(HttpUser.FindFirst(ClaimNames.ActiveTenantId)?.Value, out var value)
                ? value
                : null;

        public TenantRole? ActiveTenantRole =>
            Enum.TryParse<TenantRole>(HttpUser.FindFirst(ClaimNames.ActiveTenantRole)?.Value, out var value)
                ? value
                : null;

        public SystemRole SystemRole =>
            Enum.TryParse<SystemRole>(HttpUser.FindFirst(ClaimTypes.Role)?.Value, out var value)
                ? value
                : default;

        public List<TenantMembership>? Tenants => null;
        public List<ProjectMembership>? Projects => null;
        public List<GroupMembership>? Groups => null;

        public bool IsAuthenticated =>
            HttpUser.Identity?.IsAuthenticated ?? false;
    }

    public record TenantMembership(Guid TenantId, TenantRole Role);
    public record ProjectMembership(Guid ProjectId, ProjectRole Role);
    public record GroupMembership(Guid GroupId);
}
