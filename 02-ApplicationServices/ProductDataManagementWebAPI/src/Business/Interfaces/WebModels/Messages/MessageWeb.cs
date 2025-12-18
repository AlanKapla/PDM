namespace Business.Interfaces.WebModels.Messages
{
    public record MessageWeb(
        Guid Id,
        Guid ChatId,
        Guid UserId,
        string UserFirstName,
        string UserLastName,
        string Content,
        DateTime CreatedAt
    );
}
