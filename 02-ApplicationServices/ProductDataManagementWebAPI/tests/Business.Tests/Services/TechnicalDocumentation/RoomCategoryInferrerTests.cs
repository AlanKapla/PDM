using Business.Implementation.Services.AI.TechnicalDocumentation;
using FluentAssertions;

namespace Business.Tests.Services.TechnicalDocumentation;

public sealed class RoomCategoryInferrerTests
{
    [Theory]
    [InlineData("Wiatrołap", "komunikacja")]
    [InlineData("Łazienka", "sanitarne")]
    [InlineData("Kuchnia", "usługowe")]
    [InlineData("Salon", "mieszkalne")]
    [InlineData("Garaż", "gospodarcze")]
    public void Infer_mapsPolishRoomNames(string roomName, string expectedCategory)
    {
        RoomCategoryInferrer.Infer(roomName).Should().Be(expectedCategory);
    }
}
