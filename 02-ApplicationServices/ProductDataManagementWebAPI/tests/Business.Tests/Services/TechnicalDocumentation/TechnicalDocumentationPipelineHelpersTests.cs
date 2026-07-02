using Business.Implementation.Services.AI.TechnicalDocumentation.Pipeline;
using Business.Interfaces.Services.TechnicalDocumentation;
using FluentAssertions;

namespace Business.Tests.Services.TechnicalDocumentation;

public sealed class TechnicalDocumentationPipelineHelpersTests
{
    [Fact]
    public void ApplyAgentResult_appendsExecutionAndMergesWarnings()
    {
        TechnicalDocumentationAgentContext context = new();
        TechnicalDocumentationAgentResult result = new(
            Success: true,
            AgentName: TechnicalDocumentationPipelineAgentNames.GroupExtraction,
            Summary: "Extracted 2 groups.",
            Warnings: ["Group roof_structure has no images.", "Group roof_structure has no images."]);

        TechnicalDocumentationPipelineHelpers.ApplyAgentResult(context, result);

        context.AgentExecutions.Should().ContainSingle();
        context.AgentExecutions[0].Summary.Should().Be("Extracted 2 groups.");
        context.PipelineWarnings.Should().ContainSingle()
            .Which.Should().Be("Group roof_structure has no images.");
    }
}
