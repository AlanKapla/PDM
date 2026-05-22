namespace Business.Interfaces.WebModels.Chats.Requests;

public sealed record SendMessageRequest(string Content, Guid? ReplyToMessageId = null);
