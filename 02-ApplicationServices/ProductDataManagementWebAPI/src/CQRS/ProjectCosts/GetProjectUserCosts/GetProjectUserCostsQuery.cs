using Business.Interfaces.WebModels.ProjectCosts;
using System;
using System.Collections.Generic;

namespace CQRS.ProjectCosts.GetProjectUserCosts
{
    /// <summary>
    /// Query do pobierania listy kosztów zalogowanego użytkownika w projekcie
    /// </summary>
    public record GetProjectUserCostsQuery : IRequestQuery<IEnumerable<ProjectCostListItemWeb>>
    {
        public Guid TenantId { get; init; }
        public Guid ProjectId { get; init; }
    }
}
