using Business.Interfaces.Constants;
using Business.Interfaces.DTO;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using CQRS.Helpers;
using Entities.Enums;
using Entities.Models;
using Entities.Models.Notifications;
using Entities.Models.Roles;
using Entities.Models.Tenants;
using Entities.Models.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;
using NotificationType = Business.Interfaces.DTO.NotificationType;

namespace CQRS.Tenants.UpdateTenantMemberRole
{
    public sealed class UpdateTenantMemberRoleCommandHandler : IRequestHandler<UpdateTenantMemberRoleCommand, Unit>
    {
        private readonly IReadRepository<Tenant> tenantRepo;
        private readonly IReadRepository<User> userRepo;
        private readonly IRepository<TenantMember> tenantMemberRepo;
        private readonly IRepository<TenantPreferencesProfile> tenantPrefsRepo;
        private readonly IReadRepository<Role> roleRepo;
        private readonly IReadRepository<Notification> notificationRepo;
        private readonly IPermissionsVersionService permissionsVersionService;
        private readonly INotificationSender notificationSender;
        private readonly ICurrentUser currentUser;

        public UpdateTenantMemberRoleCommandHandler(
            IReadRepository<Tenant> tenantRepo,
            IReadRepository<User> userRepo,
            IRepository<TenantMember> tenantMemberRepo,
            IRepository<TenantPreferencesProfile> tenantPrefsRepo,
            IReadRepository<Role> roleRepo,
            IReadRepository<Notification> notificationRepo,
            IPermissionsVersionService permissionsVersionService,
            INotificationSender notificationSender,
            ICurrentUser currentUser)
        {
            this.tenantRepo = tenantRepo;
            this.userRepo = userRepo;
            this.tenantMemberRepo = tenantMemberRepo;
            this.tenantPrefsRepo = tenantPrefsRepo;
            this.roleRepo = roleRepo;
            this.notificationRepo = notificationRepo;
            this.permissionsVersionService = permissionsVersionService;
            this.notificationSender = notificationSender;
            this.currentUser = currentUser;
        }

        public async Task<Unit> Handle(UpdateTenantMemberRoleCommand request, CancellationToken cancellationToken)
        {
            Tenant tenant = await tenantRepo.GetFirstBySearch(t => t.Id == request.TenantId)
                ?? throw new NotFoundApiException(nameof(Tenant), request.TenantId.ToString());

            TenantMember tenantMember = await tenantMemberRepo.GetFirstBySearch(
                m => m.TenantId == request.TenantId
                    && m.UserId == request.UserId
                    && m.IsActive,
                q => q.Include(m => m.MemberRole))
                ?? throw new NotFoundApiException(nameof(TenantMember), $"Tenant: {request.TenantId}, User: {request.UserId}");

            Role newRole = await roleRepo.GetFirstBySearch(
                r => r.Id == request.RoleId && r.Scope == RoleScope.Tenant && r.IsActive,
                cancellationToken)
                ?? throw new NotFoundApiException(nameof(Role), request.RoleId.ToString());

            bool isDemoting = tenantMember.MemberRole?.Code == RoleCodes.TenantAdmin
                && newRole.Code != RoleCodes.TenantAdmin;

            if (isDemoting)
            {
                int adminCount = await tenantMemberRepo.CountAsync(
                    m => m.TenantId == request.TenantId
                         && m.IsActive
                         && m.MemberRole!.Code == RoleCodes.TenantAdmin,
                    cancellationToken);

                if (adminCount <= 1)
                {
                    throw new ConflictApiException(
                        nameof(TenantMember),
                        request.UserId.ToString(),
                        "Nie można zdegradować ostatniego administratora tenanta.");
                }
            }

            Guid? oldRoleId = tenantMember.RoleId;
            tenantMember.RoleId = newRole.Id;

            await tenantMemberRepo.Update(tenantMember);

            if (!tenant.IsActive && newRole.Code != RoleCodes.TenantAdmin)
            {
                TenantPreferencesProfile? userProfile = await tenantPrefsRepo.GetFirstBySearch(
                    p => p.UserId == request.UserId && p.ActiveTenantId == request.TenantId);

                if (userProfile is not null)
                {
                    userProfile.ActiveTenantId = null;
                    await tenantPrefsRepo.Update(userProfile);
                }
            }

            await permissionsVersionService.BumpVersionAsync(request.UserId, cancellationToken);

            User? targetUser = await userRepo.GetFirstBySearch(u => u.Id == request.UserId);

            NotificationDto notification = new NotificationDto
            {
                Id = Guid.NewGuid(),
                TenantId = request.TenantId,
                ProjectId = null,
                UserId = request.UserId,
                AzureAdB2CObjectId = targetUser?.AzureAdB2CObjectId,
                Type = NotificationType.Info,
                Title = "Zmieniono Twoją rolę w organizacji",
                Message = $"Twoja rola w organizacji '{tenant.Name}' została zmieniona na {newRole.Name}.",
                CreatedAt = DateTime.UtcNow,
                IsRead = false,
                Metadata = new Dictionary<string, object?>
                {
                    { "tenantId", request.TenantId },
                    { "tenantName", tenant.Name },
                    { "oldRoleId", oldRoleId },
                    { "newRoleId", newRole.Id },
                    { "newRoleCode", newRole.Code },
                    { "newRoleName", newRole.Name },
                    { "changedByUserId", currentUser.Id }
                }
            };

            NotificationPayloadDto payload = await NotificationPayloadHelper.CreatePayloadAsync(notification, notificationRepo, cancellationToken);
            await notificationSender.EnqueueAsync(payload, cancellationToken);

            return Unit.Value;
        }
    }
}
