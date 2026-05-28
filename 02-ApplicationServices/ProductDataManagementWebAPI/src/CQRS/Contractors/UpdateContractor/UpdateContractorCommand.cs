using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Contractors;

namespace CQRS.Contractors.UpdateContractor
{
    public sealed record UpdateContractorCommand : IRequestCommand<ContractorWeb>, IAuthorizableRequest
    {
        public Guid TenantId { get; init; }
        public Guid Id { get; init; }
        public required string Name { get; init; }
        public string? TaxId { get; init; }
        public string? Email { get; init; }
        public string? PhoneNumber { get; init; }
        public string? Street { get; init; }
        public string? City { get; init; }
        public string? PostalCode { get; init; }
        public string? Country { get; init; }
        public string? Notes { get; init; }

        public string PermissionCode => PermissionCodes.TenantSettingsEdit;
        public ResourceRef GetResource() => new(TenantId: TenantId);
    }
}
