using Entities.Models.Base;

namespace Entities.Models.Projects
{
    public abstract class ProjectParams : BaseEntity
    {
        public Guid ProjectId { get; set; }
        public virtual Project Project { get; set; } = default!;
    }
}
