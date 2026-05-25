using Business.Interfaces.Exceptions;
using Business.Interfaces.WebModels.Subscriptions;
using Entities.Models.Subscriptions;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.Subscriptions.GetPaymentHistory;

public sealed class GetPaymentHistoryQueryHandler
    : IRequestHandler<GetPaymentHistoryQuery, IEnumerable<SubscriptionPaymentWeb>>
{
    private readonly IReadRepository<TenantSubscription> subscriptionRepo;
    private readonly IReadRepository<SubscriptionPayment> paymentRepo;
    private readonly IReadRepository<SubscriptionPlanDefinition> planRepo;

    public GetPaymentHistoryQueryHandler(
        IReadRepository<TenantSubscription> subscriptionRepo,
        IReadRepository<SubscriptionPayment> paymentRepo,
        IReadRepository<SubscriptionPlanDefinition> planRepo)
    {
        this.subscriptionRepo = subscriptionRepo;
        this.paymentRepo      = paymentRepo;
        this.planRepo         = planRepo;
    }

    public async Task<IEnumerable<SubscriptionPaymentWeb>> Handle(
        GetPaymentHistoryQuery request,
        CancellationToken cancellationToken)
    {
        TenantSubscription? subscription = await subscriptionRepo.GetFirstBySearch(
            s => s.TenantId == request.TenantId,
            cancellationToken);

        if (subscription is null)
        {
            throw new NotFoundApiException(nameof(TenantSubscription), request.TenantId.ToString());
        }

        List<SubscriptionPayment> payments = await paymentRepo.GetPagedBySearchAsync(
            p => p.TenantId == request.TenantId,
            q => q.OrderByDescending(p => p.CreatedAt),
            take: 100,
            cancellationToken);

        List<SubscriptionPlanDefinition> planDefinitions = await GetPlanDefinitionsAsync(
            payments, cancellationToken);

        return payments.Select(p => MapToWeb(p, planDefinitions));
    }

    private async Task<List<SubscriptionPlanDefinition>> GetPlanDefinitionsAsync(
        List<SubscriptionPayment> payments,
        CancellationToken cancellationToken)
    {
        if (payments.Count == 0)
        {
            return new List<SubscriptionPlanDefinition>();
        }

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
