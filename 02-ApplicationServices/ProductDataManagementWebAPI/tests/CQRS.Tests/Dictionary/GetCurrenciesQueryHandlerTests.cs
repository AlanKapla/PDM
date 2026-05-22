using Business.Interfaces.WebModels.Dictionary;
using CQRS.Dictionary.GetCurrencies;
using FluentAssertions;

namespace CQRS.Tests.Dictionary;

public sealed class GetCurrenciesQueryHandlerTests
{
    private readonly GetCurrenciesQueryHandler _handler = new();

    [Fact]
    public async Task Handle_Always_ReturnsTenCurrencies()
    {
        // Arrange
        GetCurrenciesQuery query = new();

        // Act
        IReadOnlyList<CurrencyDictionaryItemWeb> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(10);
    }

    [Fact]
    public async Task Handle_Always_FirstCurrencyIsPln()
    {
        // Arrange
        GetCurrenciesQuery query = new();

        // Act
        IReadOnlyList<CurrencyDictionaryItemWeb> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result[0].Code.Should().Be("PLN");
    }

    [Fact]
    public async Task Handle_Always_AllCurrenciesHaveNonEmptyCode()
    {
        // Arrange
        GetCurrenciesQuery query = new();

        // Act
        IReadOnlyList<CurrencyDictionaryItemWeb> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().OnlyContain(c => !string.IsNullOrEmpty(c.Code));
    }
}
