namespace Business.Interfaces.DTO
{
    public record NotificationPayloadDto(
        NotificationDto Notification,
        int UnreadNotificationCounter
    );
}
