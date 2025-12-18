using CQRS;

namespace CQRS.Chats.CreateChat
{
    public record CreateChatCommand(
        Guid TenantId,
        Guid ProjectId,
        string Name,
        bool IsGroupChat,
        List<Guid> MemberUserIds
    ) : IRequestCommand<Guid>;
}
