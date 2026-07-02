using Business.AIAgent.Core;
using Business.Implementation.Services.AI.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation.Drawings;
using FluentAssertions;

namespace Business.Tests.Services.TechnicalDocumentation;

public sealed class TechnicalDocumentationExtractionHelperTests
{
    [Fact]
    public void BuildSystemPrompt_replacesFocusPlaceholder_withProvidedFocus()
    {
        // Arrange
        AgentDefinitionLoader loader = CreateLoader();

        // Act
        string systemPrompt = TechnicalDocumentationExtractionHelper.BuildSystemPrompt(
            loader,
            "universal-extraction-agent",
            new DrawingClassification { DrawingType = "rzut_parteru" },
            "FOCUS TEST INSTRUCTIONS",
            useFocusB: false);

        // Assert
        systemPrompt.Should().NotContain(TechnicalDocumentationExtractionHelper.FocusInstructionsPlaceholder);
        systemPrompt.Should().NotContain(TechnicalDocumentationSystemPromptBuilder.SchemaReferencePlaceholder);
        systemPrompt.Should().Contain("FOCUS TEST INSTRUCTIONS");
        systemPrompt.Should().Contain("\"materialSchedule\"");
    }

    [Fact]
    public void BuildSystemPrompt_loadsFocusFromRouter_whenFocusPromptEmpty()
    {
        // Arrange
        AgentDefinitionLoader loader = CreateLoader();

        // Act
        string systemPrompt = TechnicalDocumentationExtractionHelper.BuildSystemPrompt(
            loader,
            "universal-extraction-agent",
            new DrawingClassification { DrawingType = "rzut fundamentow" },
            focusPrompt: null,
            useFocusB: false);

        // Assert
        systemPrompt.Should().NotContain(TechnicalDocumentationExtractionHelper.FocusInstructionsPlaceholder);
        systemPrompt.Should().NotContain(TechnicalDocumentationSystemPromptBuilder.SchemaReferencePlaceholder);
        systemPrompt.Should().Contain("segments");
        systemPrompt.Should().Contain("\"project\"");
    }

    private static AgentDefinitionLoader CreateLoader()
    {
        return new AgentDefinitionLoader();
    }
}
