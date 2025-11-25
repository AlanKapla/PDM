using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Entities.Enums;
using Entities.Models;
using Microsoft.AspNetCore.Http;
using Repositiories.Repository.Interfaces;
using System.Security.Claims;

namespace Business.Implementation.Model
{
    public class CurrentUser : ICurrentUser
    {
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IReadRepository<User> userRepo;
        private readonly IReadRepository<TenantPreferencesProfile> tenantPrefsRepo;

        private bool _loaded;
        private string? _firstName;
        private string? _lastName;
        private Guid? _activeTenantIdFromProfile;

        public CurrentUser(IHttpContextAccessor httpContextAccessor,
            IReadRepository<User> userRepo,
            IReadRepository<TenantPreferencesProfile> tenantPrefsRepo)
        {
            this.httpContextAccessor = httpContextAccessor;
            this.userRepo = userRepo;
            this.tenantPrefsRepo = tenantPrefsRepo;
        }

        private ClaimsPrincipal HttpUser =>
            httpContextAccessor.HttpContext?.User
            ?? new ClaimsPrincipal(new ClaimsIdentity());

        public Guid Id =>
            Guid.TryParse(HttpUser.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var value)
                ? value
                : Guid.Empty;

        public string FirstName
        {
            get
            {
                EnsureLoaded();
                return _firstName ?? string.Empty;
            }
        }

        public string LastName
        {
            get
            {
                EnsureLoaded();
                return _lastName ?? string.Empty;
            }
        }

        public string Email =>
            HttpUser.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;

        public Guid? ActiveTenantId
        {
            get
            {
                EnsureLoaded();
                return _activeTenantIdFromProfile;
            }
        }

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

        private void EnsureLoaded()
        {
            if (_loaded)
            {
                return;
            }

            Guid userId = Id;
            if (userId == Guid.Empty)
            {
                _loaded = true;
                return;
            }

            // UWAGA: wlasciwosci nie sa async, wiec jednorazowo pobieramy dane blokujaco
            // i keszujemy w obiekcie (scoped per request) – minimalizuje to koszty.
            User? user = userRepo.GetFirstBySearch(u => u.Id == userId).GetAwaiter().GetResult();
            if (user != null)
            {
                _firstName = user.FirstName;
                _lastName = user.LastName;
            }

            TenantPreferencesProfile? prefs = tenantPrefsRepo.GetFirstBySearch(p => p.UserId == userId).GetAwaiter().GetResult();
            if (prefs != null)
            {
                _activeTenantIdFromProfile = prefs.ActiveTenantId;
            }

            _loaded = true;
        }
    }

    public record TenantMembership(Guid TenantId, TenantRole Role);
    public record ProjectMembership(Guid ProjectId, ProjectRole Role);
    public record GroupMembership(Guid GroupId);
}
