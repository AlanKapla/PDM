using Entities.Enums;
using Entities.Models.Base;

namespace Entities.Models.Activity
{
    public class UserActivityLog : BaseEntity
    {
        public UserActivityEventType EventType { get; set; }
        public string IpAddress { get; set; } = string.Empty;
        public DateTime OccurredAtUtc { get; set; }
        public string? Route { get; set; }
        public Guid? UserId { get; set; }
        public string? AzureAdB2CObjectId { get; set; }
    }
}
