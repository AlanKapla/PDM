using Business.Interfaces.Exceptions;
using Entities.Models.Subscriptions;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.Admin.Subscriptions.DeactivateSubscriptionOverride;

public sealed class DeactivateSubscriptionOverrideCommandHandler
    : IRequestHandler<DeactivateSubscriptionOverrideCommand, Unit>
{
    private readonly IReadRepository<SubscriptionOverride> overrideRepo;

    public DeactivateSubscriptionOverrideCommandHandler(IReadRepository<SubscriptionOverride> overrideRepo)
    {
        this.overrideRepo = overrideRepo;
    }

    public async Task<Unit> Handle(
        DeactivateSubscriptionOverrideCommand request,
        CancellationToken cancellationToken)
    {
        SubscriptionOverride? subscriptionOverride = await overrideRepo.GetFirstBySearch(
            o => o.Id == request.OverrideId
              && o.TenantSubscription.TenantId == request.TenantId,
            cancellationToken);

        if (subscriptionOverride is null)
        {
            throw new NotFoundApiException(nameof(SubscriptionOverride), request.OverrideId.ToString());
        }

        subscriptionOverride.IsActive = false;

        await overrideRepo.Update(subscriptionOverride);
        await overrideRepo.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
