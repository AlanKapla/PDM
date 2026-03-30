namespace Business.Interfaces.WebModels.Messages
{
    public record ChatWeb(
        Guid Id,
        string Name,
        bool IsGroupChat,
        DateTime CreatedAt,
        int MembersCount,
        DateTime? LastMessageAt
    );
}
