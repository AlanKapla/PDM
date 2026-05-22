using Business.Interfaces.WebModels.Dictionary;
using MediatR;

namespace CQRS.Dictionary.GetCurrencies
{
    public class GetCurrenciesQueryHandler : IRequestHandler<GetCurrenciesQuery, IReadOnlyList<CurrencyDictionaryItemWeb>>
    {
        private static readonly IReadOnlyList<CurrencyDictionaryItemWeb> Currencies =
            new List<CurrencyDictionaryItemWeb>
            {
                new("PLN", "Polski złoty", "zł"),
                new("EUR", "Euro", "€"),
                new("USD", "Dolar amerykański", "$"),
                new("GBP", "Funt brytyjski", "£"),
                new("CHF", "Frank szwajcarski", "Fr"),
                new("CZK", "Korona czeska", "Kč"),
                new("NOK", "Korona norweska", "kr"),
                new("SEK", "Korona szwedzka", "kr"),
                new("DKK", "Korona duńska", "kr"),
                new("JPY", "Jen japoński", "¥")
            };

        public Task<IReadOnlyList<CurrencyDictionaryItemWeb>> Handle(
            GetCurrenciesQuery request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(Currencies);
        }
    }
}
