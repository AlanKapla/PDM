using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Business.Implementation.Helpers;
using Business.Implementation.Helpers.JsonConverters;
using Business.Interfaces.WebModels.TechnicalDocumentation;
using FluentAssertions;

namespace Business.Tests.Helpers;

public sealed class JsonConverterWriteHelperProbeTests
{
    [Fact]
    public void CreateJsonTypeInfo_ignoresAttributeConverter_andSerializes()
    {
        JsonSerializerOptions options = TechnicalDocumentationJsonHelper.CreateSerializerOptions();
        AuditResult value = new() { Warnings = ["test"] };

        using MemoryStream stream = new();
        using Utf8JsonWriter writer = new(stream);
        JsonConverterWriteHelper.SerializeWithoutConverter<AuditResult, AuditResultJsonConverter>(writer, value, options);
        writer.Flush();

        string json = System.Text.Encoding.UTF8.GetString(stream.ToArray());
        json.Should().Contain("warnings");
        json.Should().Contain("test");
    }
}
