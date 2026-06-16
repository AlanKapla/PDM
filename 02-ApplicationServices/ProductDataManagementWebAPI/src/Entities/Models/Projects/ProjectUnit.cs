namespace Entities.Models.Projects
{
    public class ProjectUnit : ProjectParams
    {
        public string Code { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string? Symbol { get; set; }
        public int Order { get; set; }
    }
}
