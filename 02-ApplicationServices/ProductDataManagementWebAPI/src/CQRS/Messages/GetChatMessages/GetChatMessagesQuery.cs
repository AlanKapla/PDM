using Business.Interfaces.WebModels.Messages;
using CQRS;

namespace CQRS.Messages.GetChatMessages
{
    public record GetChatMessagesQuery(Guid ChatId, int PageNumber = 1, int PageSize = 50) : IRequestQuery<List<MessageWeb>>;
}
