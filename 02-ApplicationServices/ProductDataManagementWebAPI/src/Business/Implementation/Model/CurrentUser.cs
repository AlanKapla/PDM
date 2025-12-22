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
        private Guid _id;
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

        public Guid Id
        {
            get
            {
                EnsureLoaded();
                return _id;
            }
        }

        public string AzureAdB2CObjectId =>
            HttpUser.FindFirst(ClaimNames.Oid)?.Value ?? string.Empty;

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
            HttpUser.FindFirst(ClaimNames.PreferredUsername)?.Value ?? string.Empty;

        public Guid? ActiveTenantId
        {
            get
            {
                EnsureLoaded();
                return _activeTenantIdFromProfile;
            }
        }

        public bool IsAuthenticated =>
            HttpUser.Identity?.IsAuthenticated ?? false;

        public string? GetClaimValue(string claimType)
        {
            return HttpUser.FindFirst(claimType)?.Value;
        }

        private void EnsureLoaded()
        {
            if (_loaded)
            {
                return;
            }

            string? azureB2CObjectId = HttpUser.FindFirst(ClaimNames.Oid)?.Value;

            if (string.IsNullOrEmpty(azureB2CObjectId))
            {
                _loaded = true;
                return;
            }

            User? user = userRepo.GetFirstBySearch(u => u.AzureAdB2CObjectId == azureB2CObjectId)
                .GetAwaiter().GetResult();

            if (user != null)
            {
                _id = user.Id;
                _firstName = user.FirstName;
                _lastName = user.LastName;

                TenantPreferencesProfile? prefs = tenantPrefsRepo.GetFirstBySearch(p => p.UserId == user.Id)
                    .GetAwaiter().GetResult();
                
                if (prefs != null)
                {
                    _activeTenantIdFromProfile = prefs.ActiveTenantId;
                }
            }

            _loaded = true;
        }
    }
}
