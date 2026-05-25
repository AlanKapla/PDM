namespace Business.Interfaces.WebModels.Admin;

public sealed record AdminUserDetailsWeb(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string SystemRole,
    bool IsActive,
    DateTime CreatedAt,
    string? PhoneNumber,
    string? CompanyName,
    string? TaxId,
    string? Street,
    string? City,
    string? PostalCode,
    string? Country,
    IEnumerable<AdminUserTenantMembershipWeb> TenantMemberships
);

public sealed record AdminUserTenantMembershipWeb(
    Guid TenantId,
    string TenantName,
    string RoleName,
    DateTime JoinedAt
);
