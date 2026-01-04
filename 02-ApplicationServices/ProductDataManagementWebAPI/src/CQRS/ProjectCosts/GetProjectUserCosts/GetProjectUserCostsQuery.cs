using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.ProjectCosts;
using CQRS.Interfaces;
using System;
using System.Collections.Generic;

namespace CQRS.ProjectCosts.GetProjectUserCosts
{
    /// <summary>
    /// Query do pobierania listy kosztów zalogowanego użytkownika w projekcie
    /// </summary>
    public sealed record GetProjectUserCostsQuery(
        Guid TenantId,
        Guid ProjectId
    ) : IRequestQuery<IEnumerable<ProjectCostListItemWeb>>, IAuthorizableRequest
    {
        public string PermissionCode => PermissionCodes.ProjectResourcesWrite;
        
        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    }
}
