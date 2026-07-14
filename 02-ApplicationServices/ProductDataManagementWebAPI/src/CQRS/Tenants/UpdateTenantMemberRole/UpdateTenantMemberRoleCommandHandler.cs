using Business.Interfaces.DTO;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using CQRS.Helpers;
using Entities.Models;
using Entities.Models.Notifications;
using Entities.Models.Tenants;
using Entities.Models.Users;
using MediatR;
using Repositories.Repository.Interfaces;
using NotificationType = Business.Interfaces.DTO.NotificationType;

namespace CQRS.Tenants.UpdateTenantMemberRole
{
    public sealed class UpdateTenantMemberRoleCommandHandler : IRequestHandler<UpdateTenantMemberRoleCommand, Unit>
    {
        private readonly IReadRepository<Tenant> tenantRepo;
        private readonly IReadRepository<User> userRepo;
        private readonly IRepository<TenantMember> tenantMemberRepo;
        private readonly IReadRepository<Notification> notificationRepo;
        private readonly IPermissionsVersionService permissionsVersionService;
        private readonly INotificationSender notificationSender;
        private readonly ICurrentUser currentUser;

        public UpdateTenantMemberRoleCommandHandler(
            IReadRepository<Tenant> tenantRepo,
            IReadRepository<User> userRepo,
            IRepository<TenantMember> tenantMemberRepo,
            IReadRepository<Notification> notificationRepo,
            IPermissionsVersionService permissionsVersionService,
            INotificationSender notificationSender,
            ICurrentUser currentUser)
        {
            this.tenantRepo = tenantRepo;
            this.userRepo = userRepo;
            this.tenantMemberRepo = tenantMemberRepo;
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
                    && m.IsActive)
                ?? throw new NotFoundApiException(nameof(TenantMember), $"Tenant: {request.TenantId}, User: {request.UserId}");

            bool isDemoting = tenantMember.IsAdmin && !request.IsAdmin;

            if (isDemoting)
            {
                int adminCount = await tenantMemberRepo.CountAsync(
                    m => m.TenantId == request.TenantId
                         && m.IsActive
                         && m.IsAdmin,
                    cancellationToken);

                if (adminCount <= 1)
                {
                    throw new ConflictApiException(
                        nameof(TenantMember),
                        request.UserId.ToString(),
                        "Nie można odebrać uprawnień administratora ostatniemu administratorowi tenanta.");
                }
            }

            tenantMember.IsAdmin = request.IsAdmin;

            await tenantMemberRepo.Update(tenantMember);
            await tenantMemberRepo.SaveChangesAsync(cancellationToken);

            await permissionsVersionService.BumpVersionAsync(request.UserId, cancellationToken);

            User? targetUser = await userRepo.GetFirstBySearch(u => u.Id == request.UserId, cancellationToken);

            string message = request.IsAdmin
                ? $"Otrzymałeś uprawnienia administratora w organizacji: {tenant.Name}"
                : $"Zmieniono Twoje uprawnienia w organizacji: {tenant.Name}";

            NotificationDto notification = new NotificationDto
            {
                Id = Guid.NewGuid(),
                TenantId = request.TenantId,
                ProjectId = null,
                UserId = request.UserId,
                AzureAdB2CObjectId = targetUser?.AzureAdB2CObjectId,
                Type = NotificationType.Info,
                Title = "Zmiana uprawnień",
                Message = message,
                CreatedAt = DateTime.UtcNow,
                IsRead = false,
                Metadata = new Dictionary<string, object?>
                {
                    { "tenantId", request.TenantId },
                    { "tenantName", tenant.Name },
                    { "isAdmin", request.IsAdmin },
                    { "changedByUserId", currentUser.Id }
                }
            };

            NotificationPayloadDto payload = await NotificationPayloadHelper.CreatePayloadAsync(notification, notificationRepo, cancellationToken);
            await notificationSender.EnqueueAsync(payload, cancellationToken);

            return Unit.Value;
        }
    }
}

