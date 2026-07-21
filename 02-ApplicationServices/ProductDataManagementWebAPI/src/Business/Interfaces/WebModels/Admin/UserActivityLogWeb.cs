namespace Business.Interfaces.WebModels.Admin;

public sealed record UserActivityLogWeb(
    Guid Id,
    string EventType,
    string IpAddress,
    DateTime OccurredAtUtc,
    string? Route,
    Guid? UserId,
    string? AzureAdB2CObjectId);
