using Business.AIAgent.Core;
using Business.Implementation.Services.AI.TechnicalDocumentation;
using FluentAssertions;

namespace Business.Tests.Services.TechnicalDocumentation;

public sealed class TechnicalDocumentationSystemPromptBuilderTests
{
    [Theory]
    [InlineData("drawing-classification-agent")]
    [InlineData("universal-extraction-agent")]
    [InlineData("universal-extraction-agent-b")]
    [InlineData("material-calculation-agent")]
    [InlineData("material-orchestration-agent")]
    [InlineData("details-validation-agent")]
    [InlineData("details-validation-vision-agent")]
    public void ResolveSystemPrompt_injectsSchemaReference_forAllTechnicalDocumentationAgents(string agentName)
    {
        // Arrange
        AgentDefinitionLoader loader = new();

        // Act
        string systemPrompt = TechnicalDocumentationSystemPromptBuilder.ResolveSystemPrompt(loader, agentName);

        // Assert
        systemPrompt.Should().NotContain(TechnicalDocumentationSystemPromptBuilder.SchemaReferencePlaceholder);
        systemPrompt.Should().Contain("\"project\"");
        systemPrompt.Should().Contain("\"materialSchedule\"");
    }

    [Fact]
    public void LoadSchemaReferenceText_returnsSameContentAsJsonElement()
    {
        // Act
        string text = DetailsSchemaReferenceLoader.LoadSchemaReferenceText();

        // Assert
        text.Should().Contain("\"rooms\"");
        text.TrimStart().Should().StartWith("{");
    }
}
