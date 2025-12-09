using CQRS;

namespace CQRS.Messages.SendMessage
{
    public record SendMessageCommand(
        Guid ChatId,
        string Content
    ) : IRequestCommand<Guid>;
}
