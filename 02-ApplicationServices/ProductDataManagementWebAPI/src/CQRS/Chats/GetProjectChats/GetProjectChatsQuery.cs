using Business.Interfaces.WebModels.Messages;
using CQRS;

namespace CQRS.Chats.GetProjectChats
{
    public record GetProjectChatsQuery(Guid ProjectId) : IRequestQuery<List<ChatWeb>>;
}
