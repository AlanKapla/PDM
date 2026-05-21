using Business.Interfaces.WebModels.Dictionary;

namespace CQRS.Dictionary.GetCurrencies
{
    public record GetCurrenciesQuery : IRequestQuery<IReadOnlyList<CurrencyDictionaryItemWeb>>;
}
