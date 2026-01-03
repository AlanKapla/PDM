using Business.Implementation.Services;
using Business.Interfaces.DTO;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Entities.Enums;
using Entities.Models;
using MediatR;
using Repositiories.Repository.Interfaces;
using Repositories.Repository.Interfaces;
using NotificationType = Business.Interfaces.DTO.NotificationType;

namespace CQRS.Tenants.UpdateTenantMemberRole
{
    public class UpdateTenantMemberRoleCommandHandler : IRequestHandler<UpdateTenantMemberRoleCommand, Unit>
    {
        private readonly IReadRepository<Tenant> tenantRepo;
        private readonly IReadRepository<User> userRepo;
        private readonly IRepository<TenantMember> tenantMemberRepo;
        private readonly IReadRepository<Role> roleRepo;
        private readonly PermissionsVersionService permissionsVersionService;
        private readonly INotificationSender notificationSender;
        private readonly ICurrentUser currentUser;

        public UpdateTenantMemberRoleCommandHandler(
            IReadRepository<Tenant> tenantRepo,
            IReadRepository<User> userRepo,
            IRepository<TenantMember> tenantMemberRepo,
            IReadRepository<Role> roleRepo,
            PermissionsVersionService permissionsVersionService,
            INotificationSender notificationSender,
            ICurrentUser currentUser)
        {
            this.tenantRepo = tenantRepo;
            this.userRepo = userRepo;
            this.tenantMemberRepo = tenantMemberRepo;
            this.roleRepo = roleRepo;
            this.permissionsVersionService = permissionsVersionService;
            this.notificationSender = notificationSender;
            this.currentUser = currentUser;
        }

        public async Task<Unit> Handle(UpdateTenantMemberRoleCommand request, CancellationToken cancellationToken)
        {
            Tenant tenant = (await tenantRepo.GetFirstBySearch(t => t.Id == request.TenantId && t.IsActive))
                ?? throw new NotFoundApiException(nameof(Tenant), request.TenantId.ToString());

            TenantMember tenantMember = (await tenantMemberRepo.GetFirstBySearch(
                m => m.TenantId == request.TenantId 
                    && m.UserId == request.UserId 
                    && m.IsActive))!;

            // Verify role exists and is a Tenant scope role
            var newRole = await roleRepo.GetFirstBySearch(
                r => r.Id == request.RoleId && r.Scope == RoleScope.Tenant,
                cancellationToken);

            if (newRole == null)
                throw new NotFoundApiException("Role", request.RoleId.ToString());

            var oldRoleId = tenantMember.RoleId;
            tenantMember.RoleId = newRole.Id;

            await tenantMemberRepo.Update(tenantMember);

            // Bump permissions version for the user whose role changed
            await permissionsVersionService.BumpVersionAsync(request.UserId, cancellationToken);

            User? targetUser = await userRepo.GetFirstBySearch(u => u.Id == request.UserId);

            NotificationDto notification = new()
            {
                Id = Guid.NewGuid(),
                TenantId = request.TenantId,
                ProjectId = null,
                UserId = request.UserId,
                AzureAdB2CObjectId = targetUser?.AzureAdB2CObjectId,
                Type = NotificationType.TenantRoleChanged,
                Title = "Zmieniono Twoją rolę w organizacji",
                Message = $"Twoja rola w organizacji '{tenant.Name}' została zmieniona na {newRole.Name}.",
                CreatedAt = DateTimeOffset.UtcNow,
                Readed = false,
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

            await notificationSender.EnqueueAsync(notification, cancellationToken);

            return Unit.Value;
        }
    }
}
