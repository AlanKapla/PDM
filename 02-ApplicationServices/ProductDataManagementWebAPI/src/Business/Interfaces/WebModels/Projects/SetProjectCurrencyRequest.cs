namespace Business.Interfaces.WebModels.Projects
{
    public record SetProjectCurrencyRequest(
        string Code,
        string Name,
        string? Symbol
    );
}
