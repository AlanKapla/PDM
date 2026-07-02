using Business.Implementation.Services.AI.TechnicalDocumentation;
using FluentAssertions;
using System.Text.Json;

namespace Business.Tests.Services.TechnicalDocumentation;

public sealed class DetailsSchemaReferenceLoaderTests
{
    [Fact]
    public void LoadSchemaReference_returnsValidJsonWithProjectSection()
    {
        JsonElement schema = DetailsSchemaReferenceLoader.LoadSchemaReference();

        schema.ValueKind.Should().Be(JsonValueKind.Object);
        schema.TryGetProperty("project", out JsonElement project).Should().BeTrue();
        schema.TryGetProperty("rooms", out JsonElement rooms).Should().BeTrue();
        rooms.ValueKind.Should().Be(JsonValueKind.Array);
        schema.TryGetProperty("materialSchedule", out _).Should().BeTrue();
    }
}
