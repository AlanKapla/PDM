namespace Entities.Models.Projects
{
    public class ProjectCostCategory : ProjectParams
    {
        public string Name { get; set; } = default!;
        public string? Code { get; set; }
        public int Order { get; set; }
        public string? Color { get; set; }
    }
}
