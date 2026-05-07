using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using MediatR;

namespace CQRS.Projects.SetProjectCurrency
{
    public record SetProjectCurrencyCommand(
        Guid TenantId,
        Guid ProjectId,
        string Code,
        string Name,
        string? Symbol
    ) : IRequestCommand<Unit>, IAuthorizableRequest
    {
        public string PermissionCode => PermissionCodes.ProjectEdit;

        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    }
}
