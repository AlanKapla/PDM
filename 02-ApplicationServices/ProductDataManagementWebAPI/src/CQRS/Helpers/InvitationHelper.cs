using Entities.Enums;
using Entities.Models.Tenants;
using Repositories.Repository.Interfaces;

namespace CQRS.Helpers;

internal static class InvitationHelper
{
    public const int InvitationValidityDays = 7;

    public static DateTime NewExpiryUtc() => DateTime.UtcNow.AddDays(InvitationValidityDays);

    public static async Task ExtendPendingInvitationAsync(
        IRepository<TenantInvitation> invitationRepo,
        TenantInvitation invitation,
        CancellationToken cancellationToken)
    {
        invitation.ExpiresAt = NewExpiryUtc();
        await invitationRepo.Update(invitation);
    }

    public static async Task ClearProjectScopeAsync(
        TenantInvitation invitation,
        IRepository<TenantInvitationModulePermission> modulePermissionRepo,
        CancellationToken cancellationToken)
    {
        invitation.ProjectId = null;
        invitation.IsAdmin = false;

        foreach (TenantInvitationModulePermission permission in invitation.ModulePermissions.ToList())
        {
            await modulePermissionRepo.Delete(permission);
        }

        invitation.ModulePermissions.Clear();
    }

    public static async Task ReplaceModulePermissionsAsync(
        IRepository<TenantInvitationModulePermission> modulePermissionRepo,
        TenantInvitation invitation,
        bool isAdmin,
        IReadOnlyList<ProjectModule> modules,
        CancellationToken cancellationToken)
    {
        foreach (TenantInvitationModulePermission permission in invitation.ModulePermissions.ToList())
        {
            await modulePermissionRepo.Delete(permission);
        }

        invitation.ModulePermissions.Clear();

        IEnumerable<ProjectModule> effectiveModules = isAdmin
            ? modules
            : modules.Where(m => m != ProjectModule.Settings);

        foreach (ProjectModule module in effectiveModules)
        {
            TenantInvitationModulePermission permission = new TenantInvitationModulePermission
            {
                InvitationId = invitation.Id,
                Module = module
            };

            invitation.ModulePermissions.Add(permission);
            await modulePermissionRepo.Insert(permission);
        }
    }
}
