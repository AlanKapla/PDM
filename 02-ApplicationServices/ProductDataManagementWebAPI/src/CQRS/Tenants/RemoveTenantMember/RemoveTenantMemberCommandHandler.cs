using Business.Interfaces.DTO;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using CQRS.Helpers;
using Entities.Models;
using MediatR;
using Repositories.Repository.Interfaces;
using NotificationType = Business.Interfaces.DTO.NotificationType;

namespace CQRS.Tenants.RemoveTenantMember
{
    public class RemoveTenantMemberCommandHandler : IRequestHandler<RemoveTenantMemberCommand, Unit>
    {
        private readonly IReadRepository<Tenant> tenantRepo;
        private readonly IReadRepository<User> userRepo;
        private readonly IRepository<TenantMember> tenantMemberRepo;
        private readonly IRepository<TenantPreferencesProfile> tenantPreferencesRepo;
        private readonly IReadRepository<Notification> notificationRepo;
        private readonly INotificationSender notificationSender;
        private readonly ICurrentUser currentUser;

        public RemoveTenantMemberCommandHandler(
            IReadRepository<Tenant> tenantRepo,
            IReadRepository<User> userRepo,
            IRepository<TenantMember> tenantMemberRepo,
            IRepository<TenantPreferencesProfile> tenantPreferencesRepo,
            IReadRepository<Notification> notificationRepo,
            INotificationSender notificationSender,
            ICurrentUser currentUser)
        {
            this.tenantRepo = tenantRepo;
            this.userRepo = userRepo;
            this.tenantMemberRepo = tenantMemberRepo;
            this.tenantPreferencesRepo = tenantPreferencesRepo;
            this.notificationRepo = notificationRepo;
            this.notificationSender = notificationSender;
            this.currentUser = currentUser;
        }

        public async Task<Unit> Handle(RemoveTenantMemberCommand request, CancellationToken cancellationToken)
        {
            Tenant tenant = await tenantRepo.GetFirstBySearch(t => t.Id == request.TenantId)
                ?? throw new NotFoundApiException(nameof(Tenant), request.TenantId.ToString());

            TenantMember tenantMember = await tenantMemberRepo.GetFirstBySearch(
                m => m.TenantId == request.TenantId 
                    && m.UserId == request.UserId 
                    && m.IsActive)
                ?? throw new NotFoundApiException(nameof(TenantMember), $"Tenant: {request.TenantId}, User: {request.UserId}");

            tenantMember.IsActive = false;
            await tenantMemberRepo.Update(tenantMember);

            TenantPreferencesProfile? tenantProfile = await tenantPreferencesRepo.GetFirstBySearch(
                p => p.UserId == request.UserId && p.ActiveTenantId == request.TenantId);

            if (tenantProfile != null)
            {
                tenantProfile.ActiveTenantId = null;
                await tenantPreferencesRepo.Update(tenantProfile);
            }

            User? targetUser = await userRepo.GetFirstBySearch(u => u.Id == request.UserId, cancellationToken);

            NotificationDto notification = new()
            {
                Id = Guid.NewGuid(),
                TenantId = request.TenantId,
                ProjectId = null,
                UserId = request.UserId,
                AzureAdB2CObjectId = targetUser?.AzureAdB2CObjectId,
                Type = NotificationType.Warning,
                Title = "Usunięto z organizacji",
                Message = $"Zostałeś usunięty z organizacji: {tenant.Name}",
                CreatedAt = DateTimeOffset.UtcNow,
                Readed = false,
                Metadata = new Dictionary<string, object?>
                {
                    { "tenantId", request.TenantId },
                    { "tenantName", tenant.Name },
                    { "removedByUserId", currentUser.Id }
                }
            };

            var payload = await NotificationPayloadHelper.CreatePayloadAsync(notification, notificationRepo, cancellationToken);
            await notificationSender.EnqueueAsync(payload, cancellationToken);

            return Unit.Value;
        }
    }
}
