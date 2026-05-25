namespace Business.Interfaces.WebModels.Admin;

public sealed record AdminUserListItemWeb(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string SystemRole,
    bool IsActive,
    DateTime CreatedAt,
    int TenantCount
);
