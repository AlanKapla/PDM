namespace Business.Interfaces.DTO
{
    public class NotificationDto
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid? ProjectId { get; set; }
        public string TenantName { get; set; } = string.Empty;
        public string? ProjectName { get; set; }
        public Guid UserId { get; set; }
        public string? AzureAdB2CObjectId { get; set; }
        public NotificationType Type { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; }
        public bool Readed { get; set; }
        public Dictionary<string, object?>? Metadata { get; set; }
    }
}
