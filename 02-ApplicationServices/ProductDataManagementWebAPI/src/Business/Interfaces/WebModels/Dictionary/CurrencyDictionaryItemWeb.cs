namespace Business.Interfaces.WebModels.Dictionary
{
    public record CurrencyDictionaryItemWeb(
        string Code,
        string Name,
        string? Symbol
    );
}
