namespace Business.Interfaces.WebModels.Projects
{
    public sealed record UpdateProjectWeb
    {
        public required string Name { get; init; }
    }
}
