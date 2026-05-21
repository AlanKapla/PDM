namespace Business.Interfaces.WebModels.Chats.Requests;

public sealed record AddChatMemberRequest(Guid UserId, Guid? ProjectId = null);
