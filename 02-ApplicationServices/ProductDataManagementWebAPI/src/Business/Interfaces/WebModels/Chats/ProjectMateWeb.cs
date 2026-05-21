namespace Business.Interfaces.WebModels.Chats;

/// <summary>
/// API-facing DTO representing a user who shares at least one project with the current user.
/// Used to populate contact lists for direct and group chat creation.
/// </summary>
public sealed record ProjectMateWeb(
    Guid UserId,
    string FirstName,
    string LastName);
