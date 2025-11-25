namespace Business.Interfaces.WebModels.Tenants
{
    using Entities.Models;

    public class TenantInvitationWeb
    {
        public Guid InvitationId { get; set; }
        public Guid TenantId { get; set; }
        public string TenantName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty; // adres zaproszonego u¿ytkownika
        public string InvitedByUserEmail { get; set; } = string.Empty; // email nadawcy
        public string InvitedByUserName { get; set; } = string.Empty; // pe³na nazwa nadawcy (FirstName + LastName snapshot)
        public DateTime CreatedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public InvitationStatus Status { get; set; }
        // Token mo¿e byæ u¿yty do akceptacji z UI poprzez istniej¹cy endpoint.
        public string Token { get; set; } = string.Empty;
    }
}
