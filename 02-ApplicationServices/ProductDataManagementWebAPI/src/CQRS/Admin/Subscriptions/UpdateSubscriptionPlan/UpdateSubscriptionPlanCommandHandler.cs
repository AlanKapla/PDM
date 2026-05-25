using Business.Interfaces.Exceptions;
using Business.Interfaces.WebModels.Admin;
using Entities.Models.Subscriptions;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.Admin.Subscriptions.UpdateSubscriptionPlan;

public sealed class UpdateSubscriptionPlanCommandHandler
    : IRequestHandler<UpdateSubscriptionPlanCommand, SubscriptionPlanDefinitionWeb>
{
    private readonly IReadRepository<SubscriptionPlanDefinition> planRepo;

    public UpdateSubscriptionPlanCommandHandler(IReadRepository<SubscriptionPlanDefinition> planRepo)
    {
        this.planRepo = planRepo;
    }

    public async Task<SubscriptionPlanDefinitionWeb> Handle(
        UpdateSubscriptionPlanCommand request,
        CancellationToken cancellationToken)
    {
        SubscriptionPlanDefinition? plan = await planRepo.GetById(request.Id);

        if (plan is null)
        {
            throw new NotFoundApiException(nameof(SubscriptionPlanDefinition), request.Id.ToString());
        }

        plan.Name        = request.Name;
        plan.MaxProjects = request.MaxProjects;
        plan.MaxUsers    = request.MaxUsers;
        plan.Price       = request.Price;
        plan.Currency    = request.Currency;
        plan.IsActive    = request.IsActive;
        plan.UpdatedAt   = DateTime.UtcNow;

        await planRepo.Update(plan);
        await planRepo.SaveChangesAsync(cancellationToken);

        return new SubscriptionPlanDefinitionWeb(
            plan.Id,
            plan.Plan.ToString(),
            plan.Name,
            plan.MaxProjects,
            plan.MaxUsers,
            plan.Price,
            plan.Currency,
            plan.IsActive);
    }
}
