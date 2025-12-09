namespace Business.Interfaces.WebModels.Messages
{
    public record ChatWeb(
        Guid Id,
        Guid TenantId,
        Guid ProjectId,
        string Name,
        bool IsGroupChat,
        DateTime CreatedAt,
        int MembersCount,
        DateTime? LastMessageAt
    );
}
