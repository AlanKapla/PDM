namespace Business.Interfaces.WebModels.Admin;

public record AdminTenantDetailsWeb(
    Guid Id,
    string Name,
    bool IsActive,
    DateTime CreatedAt,
    int MemberCount,
    IEnumerable<AdminTenantProjectItemWeb> Projects);
