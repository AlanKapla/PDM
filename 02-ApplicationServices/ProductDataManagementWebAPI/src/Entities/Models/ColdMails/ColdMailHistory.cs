using Entities.Enums;
using Entities.Models.Base;
using Entities.Models.Users;

namespace Entities.Models.ColdMails
{
    public class ColdMailHistory : BaseEntity
    {
        public Guid BatchId { get; set; }
        public string RecipientEmail { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string HtmlBody { get; set; } = string.Empty;
        public ColdMailStatus Status { get; set; }
        public string? ErrorMessage { get; set; }
        public Guid SentByUserId { get; set; }
        public virtual User SentByUser { get; set; } = default!;
        public DateTime SentAt { get; set; }
    }
}
