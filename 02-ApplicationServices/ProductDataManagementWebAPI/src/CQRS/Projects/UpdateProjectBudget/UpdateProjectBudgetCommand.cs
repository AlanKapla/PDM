using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using MediatR;

namespace CQRS.Projects.UpdateProjectBudget
{
    /// <summary>
    /// Command do aktualizacji pól budżetowych (BudgetNet, BudgetGross) w projekcie.
    /// </summary>
    public sealed record UpdateProjectBudgetCommand : IRequestCommand<Unit>, IAuthorizableRequest
    {
        public decimal? BudgetNet { get; init; }
        public decimal? BudgetGross { get; init; }
        public Guid TenantId { get; init; }
        public Guid ProjectId { get; init; }

        public string PermissionCode => PermissionCodes.ProjectSettings;

        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    }
}
