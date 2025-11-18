using Entities.Models.Base;

namespace Entities.Models
{
    public class ProjectGroup : BaseEntity
    {
        public Guid ProjectId { get; set; }
        public string Name { get; set; } = default!;

        public Project Project { get; set; } = default!;
        public ICollection<ProjectGroupMember> Members { get; set; } = new List<ProjectGroupMember>();
    }
}
