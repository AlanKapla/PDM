namespace Business.Interfaces.DTO
{
    public class NotificationMarkAsReadDto
    {
        public Guid NotificationId { get; set; }
        public Guid UserId { get; set; }
        public string? AzureAdB2CObjectId { get; set; }
        public DateTimeOffset ReadAt { get; set; }
    }
}
