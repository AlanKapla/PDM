using Business.Interfaces.WebModels.Admin;

namespace CQRS.Admin.Subscriptions.GetSubscriptionPlans;

public sealed record GetSubscriptionPlansQuery : IRequestQuery<IEnumerable<SubscriptionPlanDefinitionWeb>>, ISuperAdminRequest;
