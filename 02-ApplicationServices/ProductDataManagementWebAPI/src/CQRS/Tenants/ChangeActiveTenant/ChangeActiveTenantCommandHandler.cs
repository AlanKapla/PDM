using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Tenants;
using Entities.Enums;
using Entities.Models;
using Entities.Models.Subscriptions;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.Tenants.ChangeActiveTenant
{
    public sealed class ChangeActiveTenantCommandHandler : IRequestHandler<ChangeActiveTenantCommand, ActiveTenantWeb>
    {
        private readonly IRepository<TenantPreferencesProfile> tenantPreferencesRepo;
        private readonly IReadRepository<TenantSubscription> subscriptionReadRepo;
        private readonly ICurrentUser currentUser;

        public ChangeActiveTenantCommandHandler(
            IRepository<TenantPreferencesProfile> tenantPreferencesRepo,
            IReadRepository<TenantSubscription> subscriptionReadRepo,
            ICurrentUser currentUser)
        {
            this.tenantPreferencesRepo = tenantPreferencesRepo;
            this.subscriptionReadRepo = subscriptionReadRepo;
            this.currentUser = currentUser;
        }

        public async Task<ActiveTenantWeb> Handle(ChangeActiveTenantCommand request, CancellationToken cancellationToken)
        {
            TenantPreferencesProfile? profile = await tenantPreferencesRepo.GetFirstBySearch(p => p.UserId == currentUser.Id);
            if (profile is null)
            {
                profile = new TenantPreferencesProfile
                {
                    UserId = currentUser.Id,
                    ActiveTenantId = request.TenantId
                };
                await tenantPreferencesRepo.Insert(profile);
            }
            else
            {
                profile.ActiveTenantId = request.TenantId;
                await tenantPreferencesRepo.Update(profile);
            }

            TenantSubscription? subscription = await subscriptionReadRepo.GetFirstBySearch(
                s => s.TenantId == request.TenantId,
                cancellationToken);

            if (subscription is null || !IsSubscriptionStatusBlocked(subscription.Status))
            {
                return new ActiveTenantWeb { ActiveTenantId = profile.ActiveTenantId, IsSubscriptionBlocked = false };
            }

            TenantCtxSnapshot? snapshot = await currentUser.GetTenantSnapshotAsync(request.TenantId, cancellationToken);
            bool isTenantAdmin = snapshot?.IsTenantAdmin == true || currentUser.IsSuperAdmin;

            if (isTenantAdmin)
            {
                return new ActiveTenantWeb { ActiveTenantId = profile.ActiveTenantId, IsSubscriptionBlocked = true };
            }

            throw new SubscriptionSuspendedException(request.TenantId);
        }

        private static bool IsSubscriptionStatusBlocked(SubscriptionStatus status)
            => status is SubscriptionStatus.PastDue or SubscriptionStatus.Canceled or SubscriptionStatus.GracePeriod;
    }
}
