using Business.Interfaces.WebModels.Dictionary;
using MediatR;

namespace CQRS.Dictionary.GetCurrencies
{
    public record GetCurrenciesQuery : IRequest<IReadOnlyList<CurrencyDictionaryItemWeb>>;
}
