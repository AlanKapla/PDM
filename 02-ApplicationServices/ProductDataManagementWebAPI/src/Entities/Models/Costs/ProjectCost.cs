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
        /// Status akceptacji kosztu
        /// </summary>
        public CostApprovalStatus ApprovalStatus { get; set; } = CostApprovalStatus.Draft;

        /// <summary>
        /// ID użytkownika, który zaakceptował koszt
        /// </summary>
        public Guid? ApprovedByUserId { get; set; }

        /// <summary>
        /// Data akceptacji kosztu
        /// </summary>
        public DateTime? ApprovedAt { get; set; }

        // Navigation
        public virtual ProjectMember ProjectMember { get; set; } = default!;
    }
}
