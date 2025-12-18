using Microsoft.AspNetCore.Authorization;

namespace WebApi.Authorization
{
    /// <summary>
    /// Wymaganie sprawdzające, czy użytkownik jest adminem tenanta.
    /// W przeciwieństwie do TenantAdminRequirement nie wymaga aktywnego tenanta,
    /// co pozwala na zarządzanie nieaktywnymi tenantami.
    /// </summary>
    public class TenantAdminOrOwnerRequirement : IAuthorizationRequirement { }
}
