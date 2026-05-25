namespace Business.Interfaces.WebModels.Admin;

public record AdminTenantProjectItemWeb(
    Guid Id,
    string Name,
    bool IsActive,
    DateTime CreatedAt,
    int MemberCount,
    decimal? BudgetNet,
    decimal? BudgetGross);
