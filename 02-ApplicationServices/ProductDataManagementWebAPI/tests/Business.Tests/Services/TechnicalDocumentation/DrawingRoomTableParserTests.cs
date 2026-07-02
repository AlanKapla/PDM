using Business.Implementation.Services.AI.TechnicalDocumentation;
using FluentAssertions;

namespace Business.Tests.Services.TechnicalDocumentation;

public sealed class DrawingRoomTableParserTests
{
    [Fact]
    public void Parse_extractsRoomsFromSemicolonSeparatedTable()
    {
        string table =
            "21-Klatka schodowa-6.32m2; 22-Przedpokój-18.51m2; 26-Pokój-15.01m2";

        List<Business.Interfaces.WebModels.TechnicalDocumentation.Drawings.Room> rooms =
            DrawingRoomTableParser.Parse(table);

        rooms.Should().HaveCount(3);
        rooms[2].Symbol.Should().Be("26");
        rooms[2].Name.Should().Be("Pokój");
        rooms[2].AreaM2.Should().Be(15.01);
    }
}
