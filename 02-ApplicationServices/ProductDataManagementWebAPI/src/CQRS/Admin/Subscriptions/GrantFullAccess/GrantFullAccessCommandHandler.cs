using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Admin;
using Entities.Models.Subscriptions;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.Admin.Subscriptions.GrantFullAccess;

public sealed class GrantFullAccessCommandHandler
    : IRequestHandler<GrantFullAccessCommand, GrantFullAccessResultWeb>
{
    private readonly IReadRepository<TenantSubscription> subscriptionRepo;
    private readonly ICurrentUser currentUser;

    public GrantFullAccessCommandHandler(
        IReadRepository<TenantSubscription> subscriptionRepo,
        ICurrentUser currentUser)
    {
        this.subscriptionRepo = subscriptionRepo;
        this.currentUser      = currentUser;
    }

    public async Task<GrantFullAccessResultWeb> Handle(
        GrantFullAccessCommand request,
        CancellationToken cancellationToken)
    {
        TenantSubscription? subscription = await subscriptionRepo.GetFirstBySearch(
            s => s.TenantId == request.TenantId,
            cancellationToken);

        if (subscription is null)
        {
            throw new NotFoundApiException(nameof(TenantSubscription), request.TenantId.ToString());
        }

        if (subscription.IsFullAccess)
        {
            return new GrantFullAccessResultWeb(
                subscription.FullAccessGrantedAt!.Value,
                subscription.FullAccessGrantedByAdminId!.Value);
        }

        subscription.GrantFullAccess(currentUser.Id);

        await subscriptionRepo.Update(subscription);
        await subscriptionRepo.SaveChangesAsync(cancellationToken);

        return new GrantFullAccessResultWeb(
            subscription.FullAccessGrantedAt!.Value,
            subscription.FullAccessGrantedByAdminId!.Value);
    }
}
