using Business.Interfaces.Exceptions;
using Business.Interfaces.WebModels.Admin;
using Entities.Models.Subscriptions;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.Admin.Subscriptions.UpdatePlanDefinition;

public sealed class UpdatePlanDefinitionCommandHandler
    : IRequestHandler<UpdatePlanDefinitionCommand, AdminSubscriptionPlanWeb>
{
    private readonly IReadRepository<SubscriptionPlanDefinition> planRepo;

    public UpdatePlanDefinitionCommandHandler(IReadRepository<SubscriptionPlanDefinition> planRepo)
    {
        this.planRepo = planRepo;
    }

    public async Task<AdminSubscriptionPlanWeb> Handle(
        UpdatePlanDefinitionCommand request,
        CancellationToken cancellationToken)
    {
        SubscriptionPlanDefinition? plan = await planRepo.GetFirstBySearch(
            p => p.Plan == request.Plan,
            cancellationToken);

        if (plan is null)
        {
            throw new NotFoundApiException(nameof(SubscriptionPlanDefinition), request.Plan.ToString());
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

        return new AdminSubscriptionPlanWeb(
            plan.Id,
            (int)plan.Plan,
            plan.Name,
            plan.MaxProjects,
            plan.MaxUsers,
            plan.Price,
            plan.Currency,
            plan.IsActive,
            plan.UpdatedAt);
    }
}
