using Business.Interfaces.Services;
using Business.Interfaces.WebModels.Subscriptions;
using Entities.Models.Subscriptions;
using MediatR;

namespace CQRS.Subscriptions.ProcessMockPayment;

public sealed class ProcessMockPaymentCommandHandler
    : IRequestHandler<ProcessMockPaymentCommand, MockPaymentResultWeb>
{
    private readonly ISubscriptionBillingService billingService;

    public ProcessMockPaymentCommandHandler(ISubscriptionBillingService billingService)
    {
        this.billingService = billingService;
    }

    public async Task<MockPaymentResultWeb> Handle(
        ProcessMockPaymentCommand request,
        CancellationToken cancellationToken)
    {
        SubscriptionPayment payment = await billingService.ProcessMockPaymentAsync(
            request.TenantId, cancellationToken);

        return MapToWeb(payment);
    }

    private static MockPaymentResultWeb MapToWeb(SubscriptionPayment payment) =>
        new(
            PaymentId:      payment.Id,
            Amount:         payment.Amount,
            Currency:       payment.Currency,
            Status:         payment.Status.ToString(),
            PaidAt:         payment.PaidAt!.Value,
            PeriodEnd:      payment.PeriodEnd,
            NextPaymentDue: payment.PeriodEnd);
}
