namespace Entities.Models.Projects
{
    public class ProjectCurrency : ProjectParams
    {
        public string Code { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string? Symbol { get; set; }
    }
}
