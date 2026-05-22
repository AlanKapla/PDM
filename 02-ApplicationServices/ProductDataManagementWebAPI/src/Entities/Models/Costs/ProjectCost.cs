using Entities.Models.Projects;

namespace Entities.Models.Costs
{
    /// <summary>
    /// Reprezentuje koszt poniesiony przez członka projektu
    /// </summary>
    public class ProjectCost : BaseCost
    {
        /// <summary>
        /// ID użytkownika, który dodał koszt (członek projektu)
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// Czy koszt został zaakceptowany (włączony do trackera)
        /// </summary>
        public bool IsAccepted { get; set; } = false;

        /// <summary>
        /// ID użytkownika, który zaakceptował koszt
        /// </summary>
        public Guid? AcceptedByUserId { get; set; }

        /// <summary>
        /// Data akceptacji kosztu
        /// </summary>
        public DateTime? AcceptedAt { get; set; }

        // Navigation
        public virtual ProjectMember ProjectMember { get; set; } = default!;
        public virtual ICollection<SharedProjectCost> SharedWith { get; set; } = new List<SharedProjectCost>();
    }
}
