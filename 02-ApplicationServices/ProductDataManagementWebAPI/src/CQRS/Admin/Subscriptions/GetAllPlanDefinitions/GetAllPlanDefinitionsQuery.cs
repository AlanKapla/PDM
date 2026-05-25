using Business.Interfaces.WebModels.Admin;

namespace CQRS.Admin.Subscriptions.GetAllPlanDefinitions;

public sealed record GetAllPlanDefinitionsQuery
    : IRequestQuery<IEnumerable<AdminSubscriptionPlanWeb>>, ISuperAdminRequest;
