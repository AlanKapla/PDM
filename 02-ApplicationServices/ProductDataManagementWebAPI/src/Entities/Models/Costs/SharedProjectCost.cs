using Entities.Models.Base;
using Entities.Models.Tenants;

namespace Entities.Models.Costs
{
    /// <summary>
    /// Reprezentuje udostępnienie kosztu projektu członkowi projektu
    /// </summary>
    public class SharedProjectCost : BaseEntity
    {
        public Guid TenantId { get; set; }
        public Guid ProjectId { get; set; }
        
        /// <summary>
        /// ID kosztu, który został udostępniony
        /// </summary>
        public Guid ProjectCostId { get; set; }
        
        /// <summary>
        /// ID użytkownika, któremu udostępniono koszt
        /// </summary>
        public Guid SharedWithUserId { get; set; }
        
        /// <summary>
        /// ID użytkownika, który udostępnił koszt
        /// </summary>
        public Guid SharedByUserId { get; set; }
        
        /// <summary>
        /// Data udostępnienia
        /// </summary>
        public DateTime SharedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public ProjectCost ProjectCost { get; set; } = default!;
        public TenantMember SharedWithTenantMember { get; set; } = default!;
        public TenantMember SharedByTenantMember { get; set; } = default!;
    }
}
