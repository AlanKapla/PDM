namespace Business.Interfaces.WebModels.Chats;

/// <summary>
/// A user who can be added to an existing group chat (member of the chat's project but not yet in the chat).
/// </summary>
public sealed record AvailableMemberWeb(Guid UserId, string FirstName, string LastName);
