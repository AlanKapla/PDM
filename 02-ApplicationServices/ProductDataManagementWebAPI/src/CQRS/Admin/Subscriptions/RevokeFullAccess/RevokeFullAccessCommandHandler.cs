using Business.Interfaces.Exceptions;
using Entities.Models.Subscriptions;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.Admin.Subscriptions.RevokeFullAccess;

public sealed class RevokeFullAccessCommandHandler
    : IRequestHandler<RevokeFullAccessCommand, Unit>
{
    private readonly IReadRepository<TenantSubscription> subscriptionRepo;

    public RevokeFullAccessCommandHandler(IReadRepository<TenantSubscription> subscriptionRepo)
    {
        this.subscriptionRepo = subscriptionRepo;
    }

    public async Task<Unit> Handle(
        RevokeFullAccessCommand request,
        CancellationToken cancellationToken)
    {
        TenantSubscription? subscription = await subscriptionRepo.GetFirstBySearch(
            s => s.TenantId == request.TenantId,
            cancellationToken);

        if (subscription is null)
        {
            throw new NotFoundApiException(nameof(TenantSubscription), request.TenantId.ToString());
        }

        if (!subscription.IsFullAccess)
        {
            return Unit.Value;
        }

        subscription.RevokeFullAccess();

        await subscriptionRepo.Update(subscription);
        await subscriptionRepo.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
