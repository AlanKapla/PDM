namespace Business.Interfaces.WebModels.Admin;

public sealed record GrantFullAccessResultWeb(
    DateTime GrantedAt,
    Guid GrantedByAdminId);
