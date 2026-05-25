using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Admin;
using Entities.Models.Subscriptions;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.Admin.Subscriptions.AddSubscriptionOverride;

public sealed class AddSubscriptionOverrideCommandHandler
    : IRequestHandler<AddSubscriptionOverrideCommand, AddedSubscriptionOverrideWeb>
{
    private readonly IReadRepository<TenantSubscription> subscriptionRepo;
    private readonly IRepository<SubscriptionOverride> overrideRepo;
    private readonly ICurrentUser currentUser;

    public AddSubscriptionOverrideCommandHandler(
        IReadRepository<TenantSubscription> subscriptionRepo,
        IRepository<SubscriptionOverride> overrideRepo,
        ICurrentUser currentUser)
    {
        this.subscriptionRepo = subscriptionRepo;
        this.overrideRepo     = overrideRepo;
        this.currentUser      = currentUser;
    }

    public async Task<AddedSubscriptionOverrideWeb> Handle(
        AddSubscriptionOverrideCommand request,
        CancellationToken cancellationToken)
    {
        TenantSubscription? subscription = await subscriptionRepo.GetFirstBySearch(
            s => s.TenantId == request.TenantId,
            cancellationToken);

        if (subscription is null)
        {
            throw new NotFoundApiException(nameof(TenantSubscription), request.TenantId.ToString());
        }

        DateTime now = DateTime.UtcNow;

        SubscriptionOverride newOverride = new()
        {
            Id                   = Guid.NewGuid(),
            TenantSubscriptionId = subscription.Id,
            Key                  = request.Key,
            Value                = request.Value,
            Reason               = request.Reason,
            SetByAdminId         = currentUser.Id,
            ExpiresAt            = request.ExpiresAt,
            IsActive             = true,
            CreatedAt            = now
        };

        await overrideRepo.Insert(newOverride);
        await overrideRepo.SaveChangesAsync(cancellationToken);

        return new AddedSubscriptionOverrideWeb(
            newOverride.Id,
            newOverride.Key,
            newOverride.Value,
            newOverride.Reason,
            newOverride.ExpiresAt,
            newOverride.CreatedAt);
    }
}
