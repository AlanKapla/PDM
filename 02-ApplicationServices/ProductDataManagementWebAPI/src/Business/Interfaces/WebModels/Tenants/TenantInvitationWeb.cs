namespace Business.Interfaces.WebModels.Tenants
{
    using Entities.Models.Chats;
using Entities.Models.Costs;
using Entities.Models.Files;
using Entities.Models.Notifications;
using Entities.Models.Projects;
using Entities.Models.Roles;
using Entities.Models.Tenants;
using Entities.Models.Users;
using Entities.Models.WorkSchedules;

    public class TenantInvitationWeb
    {
        public Guid InvitationId { get; set; }
        public Guid TenantId { get; set; }
        public string TenantName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty; // adres zaproszonego u�ytkownika
        public string InvitedByUserEmail { get; set; } = string.Empty; // email nadawcy
        public string InvitedByUserName { get; set; } = string.Empty; // pe�na nazwa nadawcy (FirstName + LastName snapshot)
        public DateTime CreatedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public InvitationStatus Status { get; set; }
        // Token mo�e by� u�yty do akceptacji z UI poprzez istniej�cy endpoint.
        public string Token { get; set; } = string.Empty;
    }
}
