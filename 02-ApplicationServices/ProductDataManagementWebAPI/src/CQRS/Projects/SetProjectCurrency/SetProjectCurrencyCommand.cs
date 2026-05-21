using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using MediatR;

namespace CQRS.Projects.SetProjectCurrency
{
    public sealed record SetProjectCurrencyCommand : IRequestCommand<Unit>, IAuthorizableRequest
    {
        public required Guid TenantId { get; init; }
        public required Guid ProjectId { get; init; }
        public required string Code { get; init; }
        public required string Name { get; init; }
        public string? Symbol { get; init; }

        public string PermissionCode => PermissionCodes.ProjectEdit;

        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    }
}
