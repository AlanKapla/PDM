using Business.Interfaces.Constants;
using Business.Interfaces.DTO;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using CQRS.Helpers;
using Entities.Models;
using MediatR;
using Repositories.Repository.Interfaces;
using NotificationType = Business.Interfaces.DTO.NotificationType;

namespace CQRS.Tenants.ToggleTenantStatus;

public class ToggleTenantStatusCommandHandler : IRequestHandler<ToggleTenantStatusCommand, Unit>
{
    private readonly IRepository<Tenant> tenantRepo;
    private readonly IReadRepository<User> userRepo;
    private readonly IRepository<TenantMember> tenantMemberRepo;
    private readonly IRepository<TenantPreferencesProfile> tenantPrefsRepo;
    private readonly IReadRepository<Role> roleRepo;
    private readonly IReadRepository<Notification> notificationRepo;
    private readonly INotificationSender notificationSender;
    private readonly ICurrentUser currentUser;

    public ToggleTenantStatusCommandHandler(
        IRepository<Tenant> tenantRepo,
        IReadRepository<User> userRepo,
        IRepository<TenantMember> tenantMemberRepo,
        IRepository<TenantPreferencesProfile> tenantPrefsRepo,
        IReadRepository<Role> roleRepo,
        IReadRepository<Notification> notificationRepo,
        INotificationSender notificationSender,
        ICurrentUser currentUser)
    {
        this.tenantRepo = tenantRepo;
        this.userRepo = userRepo;
        this.tenantMemberRepo = tenantMemberRepo;
        this.tenantPrefsRepo = tenantPrefsRepo;
        this.roleRepo = roleRepo;
        this.notificationRepo = notificationRepo;
        this.notificationSender = notificationSender;
        this.currentUser = currentUser;
    }

    public async Task<Unit> Handle(ToggleTenantStatusCommand request, CancellationToken cancellationToken)
    {
        Tenant? tenant = await tenantRepo.GetFirstBySearch(t => t.Id == request.TenantId)
            ?? throw new NotFoundApiException(nameof(Tenant), request.TenantId.ToString());

        tenant.IsActive = request.IsActive;
        await tenantRepo.Update(tenant);

        if (!request.IsActive)
        {
            IEnumerable<TenantMember> allMembers = await tenantMemberRepo.GetBySearch(
                tm => tm.TenantId == request.TenantId && tm.IsActive);

            var memberRoleIds = allMembers.Where(m => m.RoleId.HasValue).Select(m => m.RoleId!.Value).Distinct().ToList();
            var roles = await roleRepo.GetBySearch(r => memberRoleIds.Contains(r.Id));
            var roleDict = roles.ToDictionary(r => r.Id);

            IEnumerable<TenantPreferencesProfile> profilesWithActiveTenant = await tenantPrefsRepo.GetBySearch(
                p => p.ActiveTenantId == request.TenantId);

            foreach (TenantPreferencesProfile profile in profilesWithActiveTenant)
            {
                TenantMember? membership = allMembers.FirstOrDefault(m => m.UserId == profile.UserId);
                
                if (membership != null && membership.RoleId.HasValue && roleDict.TryGetValue(membership.RoleId.Value, out var role))
                {
                    bool isAdmin = role.Code == RoleCodes.TenantAdmin;

                    if (!isAdmin)
                    {
                        profile.ActiveTenantId = null;
                        await tenantPrefsRepo.Update(profile);
                    }
                }
            }
        }

        IEnumerable<TenantMember> tenantMembers = await tenantMemberRepo.GetBySearch(
            tm => tm.TenantId == request.TenantId && tm.IsActive);

        string actionText = request.IsActive ? "aktywowana" : "zdezaktywowana";
        NotificationType notificationType = request.IsActive ? NotificationType.Info : NotificationType.Warning;

        var memberUserIds = tenantMembers
            .Where(tm => tm.UserId != currentUser.Id)
            .Select(tm => tm.UserId)
            .ToList();

        var users = await userRepo.GetBySearch(u => memberUserIds.Contains(u.Id));
        var userDict = users.ToDictionary(u => u.Id);

        foreach (TenantMember member in tenantMembers.Where(tm => tm.UserId != currentUser.Id))
        {
            userDict.TryGetValue(member.UserId, out User? targetUser);

            NotificationDto notification = new()
            {
                Id = Guid.NewGuid(),
                TenantId = request.TenantId,
                ProjectId = null,
                UserId = member.UserId,
                AzureAdB2CObjectId = targetUser?.AzureAdB2CObjectId,
                Type = notificationType,
                Title = $"Organizacja {actionText}",
                Message = $"Organizacja \"{tenant.Name}\" została {actionText}",
                CreatedAt = DateTimeOffset.UtcNow,
                Readed = false,
                Metadata = new Dictionary<string, object?>
                {
                    { "tenantId", request.TenantId },
                    { "tenantName", tenant.Name },
                    { "modifiedByUserId", currentUser.Id },
                    { "isActive", request.IsActive }
                }
            };

            var payload = await NotificationPayloadHelper.CreatePayloadAsync(notification, notificationRepo, cancellationToken);
            await notificationSender.EnqueueAsync(payload, cancellationToken);
        }

        return Unit.Value;
    }
}
