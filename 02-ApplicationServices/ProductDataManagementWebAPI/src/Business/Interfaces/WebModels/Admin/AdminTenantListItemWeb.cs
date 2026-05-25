namespace Business.Interfaces.WebModels.Admin;

public record AdminTenantListItemWeb(
    Guid Id,
    string Name,
    bool IsActive,
    DateTime CreatedAt,
    int MemberCount,
    int ProjectCount);
