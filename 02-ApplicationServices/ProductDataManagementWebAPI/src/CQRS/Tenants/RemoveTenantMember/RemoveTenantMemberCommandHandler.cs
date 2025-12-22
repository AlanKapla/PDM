using Business.Interfaces.DTO;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Entities.Models;
using MediatR;
using Repositiories.Repository.Interfaces;
using Repositories.Repository.Interfaces;
using NotificationType = Business.Interfaces.DTO.NotificationType;

namespace CQRS.Tenants.RemoveTenantMember
{
    public class RemoveTenantMemberCommandHandler : IRequestHandler<RemoveTenantMemberCommand, Unit>
    {
        private readonly IReadRepository<Tenant> tenantRepo;
        private readonly IReadRepository<User> userRepo;
        private readonly IRepository<TenantMember> tenantMemberRepo;
        private readonly INotificationSender notificationSender;
        private readonly ICurrentUser currentUser;

        public RemoveTenantMemberCommandHandler(
            IReadRepository<Tenant> tenantRepo,
            IReadRepository<User> userRepo,
            IRepository<TenantMember> tenantMemberRepo,
            INotificationSender notificationSender,
            ICurrentUser currentUser)
        {
            this.tenantRepo = tenantRepo;
            this.userRepo = userRepo;
            this.tenantMemberRepo = tenantMemberRepo;
            this.notificationSender = notificationSender;
            this.currentUser = currentUser;
        }

        public async Task<Unit> Handle(RemoveTenantMemberCommand request, CancellationToken cancellationToken)
        {
            // Pobierz tenant do użycia w notyfikacji (walidacja już wykonana w validatorze)
            Tenant tenant = (await tenantRepo.GetFirstBySearch(t => t.Id == request.TenantId && t.IsActive))
                ?? throw new NotFoundApiException(nameof(Tenant), request.TenantId.ToString());

            // Pobierz członka tenanta
            TenantMember tenantMember = (await tenantMemberRepo.GetFirstBySearch(
                m => m.TenantId == request.TenantId 
                    && m.UserId == request.UserId 
                    && m.IsActive))!;

            // Ustaw IsActive na false
            tenantMember.IsActive = false;
            await tenantMemberRepo.Update(tenantMember);

            User? targetUser = await userRepo.GetFirstBySearch(u => u.Id == request.UserId, cancellationToken);

            // Wyślij notyfikację do usuniętego użytkownika
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

            await notificationSender.EnqueueAsync(notification, cancellationToken);

            return Unit.Value;
        }
    }
}
