using Business.Interfaces.Exceptions;
using Business.Interfaces.WebModels.Subscriptions;
using Entities.Models.Subscriptions;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.Admin.Subscriptions.GetAdminPaymentHistory;

public sealed class GetAdminPaymentHistoryQueryHandler
    : IRequestHandler<GetAdminPaymentHistoryQuery, IEnumerable<SubscriptionPaymentWeb>>
{
    private readonly IReadRepository<TenantSubscription> subscriptionRepo;
    private readonly IReadRepository<SubscriptionPayment> paymentRepo;
    private readonly IReadRepository<SubscriptionPlanDefinition> planRepo;

    public GetAdminPaymentHistoryQueryHandler(
        IReadRepository<TenantSubscription> subscriptionRepo,
        IReadRepository<SubscriptionPayment> paymentRepo,
        IReadRepository<SubscriptionPlanDefinition> planRepo)
    {
        this.subscriptionRepo = subscriptionRepo;
        this.paymentRepo      = paymentRepo;
        this.planRepo         = planRepo;
    }

    public async Task<IEnumerable<SubscriptionPaymentWeb>> Handle(
        GetAdminPaymentHistoryQuery request,
        CancellationToken cancellationToken)
    {
        bool exists = await subscriptionRepo.GetFirstBySearch(
            s => s.TenantId == request.TenantId,
            cancellationToken) is not null;

        if (!exists)
        {
            throw new NotFoundApiException(nameof(TenantSubscription), request.TenantId.ToString());
        }

        List<SubscriptionPayment> payments = await paymentRepo.GetPagedBySearchAsync(
            p => p.TenantId == request.TenantId,
            q => q.OrderByDescending(p => p.CreatedAt),
            take: 200,
            cancellationToken);

        List<SubscriptionPlanDefinition> planDefinitions = await GetPlanDefinitionsAsync(
            payments, cancellationToken);

        return payments.Select(p => MapToWeb(p, planDefinitions));
    }

    private async Task<List<SubscriptionPlanDefinition>> GetPlanDefinitionsAsync(
        List<SubscriptionPayment> payments,
        CancellationToken cancellationToken)
    {
        Dictionary<Entities.Enums.SubscriptionPlan, SubscriptionPlanDefinition> result = new();

        foreach (Entities.Enums.SubscriptionPlan plan in payments.Select(p => p.Plan).Distinct())
        {
            SubscriptionPlanDefinition? def = await planRepo.GetFirstBySearch(
                pd => pd.Plan == plan,
                cancellationToken);

            if (def is not null)
            {
                result[plan] = def;
            }
        }

        return result.Values.ToList();
    }

    private static SubscriptionPaymentWeb MapToWeb(
        SubscriptionPayment payment,
        List<SubscriptionPlanDefinition> planDefinitions)
    {
        SubscriptionPlanDefinition? planDef = planDefinitions
            .FirstOrDefault(p => p.Plan == payment.Plan);

        return new SubscriptionPaymentWeb(
            Id:            payment.Id,
            Plan:          (int)payment.Plan,
            PlanName:      planDef?.Name ?? payment.Plan.ToString(),
            Amount:        payment.Amount,
            Currency:      payment.Currency,
            Status:        (int)payment.Status,
            StatusLabel:   payment.Status.ToString(),
            PeriodStart:   payment.PeriodStart,
            PeriodEnd:     payment.PeriodEnd,
            PaidAt:        payment.PaidAt,
            FailureReason: payment.FailureReason,
            CreatedAt:     payment.CreatedAt);
    }
}
