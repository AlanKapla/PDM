using CQRS;

namespace CQRS.Messages.MarkMessagesAsRead
{
    public record MarkMessagesAsReadCommand(
        Guid ChatId
    ) : IRequestCommand<int>;
}
