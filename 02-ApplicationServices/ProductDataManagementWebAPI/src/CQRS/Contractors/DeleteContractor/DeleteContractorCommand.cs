using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using MediatR;

namespace CQRS.Contractors.DeleteContractor
{
    public sealed record DeleteContractorCommand : IRequestCommand<Unit>, IAuthorizableRequest
    {
        public required Guid TenantId { get; init; }
        public required Guid Id { get; init; }

        public string PermissionCode => PermissionCodes.TenantEdit;
        public ResourceRef GetResource() => new(TenantId: TenantId);
    }
}
