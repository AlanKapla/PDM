namespace Business.Interfaces.WebModels.Chats.Requests;

public sealed record CreateChatRequest(Guid? ProjectId, List<Guid> MemberUserIds, string? Name = null);
