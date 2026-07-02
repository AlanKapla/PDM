using Business.AIAgent.Services;
using Business.Implementation.Services.AI.TechnicalDocumentation.Pipeline;
using Business.Interfaces.Configurations;
using Business.Interfaces.Services;
using Business.Interfaces.Services.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation.Materials;
using Business.Interfaces.WebModels.TechnicalDocumentation.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Business.Tests.Services.TechnicalDocumentation;

public sealed class GroupPipelineIntegrationTests
{
    private const double K02GroundTruthSteelKg = 1170.30;

    [Fact]
    public async Task RunAsync_executesAllGroupPipelinePhases_inOrder()
    {
        List<string> executionOrder = new();
        GroupTechnicalDocumentationPipelineRunner runner = CreateRunner(
            new RecordingGroupAgent(TechnicalDocumentationPipelineAgentNames.Ingestion, executionOrder),
            new RecordingGroupAgent(TechnicalDocumentationPipelineAgentNames.Classification, executionOrder),
            new RecordingGroupAgent(TechnicalDocumentationPipelineAgentNames.Grouping, executionOrder),
            new RecordingGroupAgent(TechnicalDocumentationPipelineAgentNames.GroupExtraction, executionOrder),
            new RecordingGroupAgent(TechnicalDocumentationPipelineAgentNames.Verification, executionOrder),
            new RecordingGroupAgent(TechnicalDocumentationPipelineAgentNames.Consolidation, executionOrder),
            new RecordingGroupAgent(TechnicalDocumentationPipelineAgentNames.MaterialsCalculation, executionOrder),
            new RecordingGroupAgent(TechnicalDocumentationPipelineAgentNames.Audit, executionOrder),
            new RecordingGroupAgent(TechnicalDocumentationPipelineAgentNames.Output, executionOrder));

        await runner.RunAsync(
            [new TechnicalDocumentationImageInput([], "K-02.pdf", 1)],
            CancellationToken.None);

        executionOrder.Should().ContainInOrder(
            TechnicalDocumentationPipelineAgentNames.Ingestion,
            TechnicalDocumentationPipelineAgentNames.Classification,
            TechnicalDocumentationPipelineAgentNames.Grouping,
            TechnicalDocumentationPipelineAgentNames.GroupExtraction,
            TechnicalDocumentationPipelineAgentNames.Verification,
            TechnicalDocumentationPipelineAgentNames.Consolidation,
            TechnicalDocumentationPipelineAgentNames.MaterialsCalculation,
            TechnicalDocumentationPipelineAgentNames.Audit,
            TechnicalDocumentationPipelineAgentNames.Output);
    }

    [Fact]
    public async Task RunAsync_K02_groundTruth_preservesSteelMassInMaterialSchedule()
    {
        GroupTechnicalDocumentationPipelineRunner runner = CreateRunner(
            new RecordingGroupAgent(TechnicalDocumentationPipelineAgentNames.Ingestion, null),
            new RecordingGroupAgent(TechnicalDocumentationPipelineAgentNames.Classification, null),
            new RecordingGroupAgent(TechnicalDocumentationPipelineAgentNames.Grouping, null),
            new RecordingGroupAgent(TechnicalDocumentationPipelineAgentNames.GroupExtraction, null),
            new RecordingGroupAgent(TechnicalDocumentationPipelineAgentNames.Verification, null),
            new K02ConsolidationStubAgent(),
            new K02MaterialsCalculationStubAgent(),
            new RecordingGroupAgent(TechnicalDocumentationPipelineAgentNames.Audit, null),
            new OutputPipelineAgent(new StubTokenUsageRecorder(), NullLogger<OutputPipelineAgent>.Instance));

        ProjectTechnicalDocumentationDetails details = await runner.RunAsync(
            [new TechnicalDocumentationImageInput([], "K-02.pdf", 1)],
            CancellationToken.None);

        details.ProjectModel.Should().NotBeNull();
        details.ProjectModel!.Slab.Should().NotBeNull();
        details.ProjectModel.Slab!.SteelBottomKg.Should().Be(K02GroundTruthSteelKg);

        details.MaterialSchedule.Should().NotBeNull();
        details.MaterialSchedule!.Groups.Slabs.Should().NotBeNull();
        details.MaterialSchedule.Groups.Slabs!.Steel.Should().ContainSingle();
        details.MaterialSchedule.Groups.Slabs.Steel[0].GrossKg.Should().Be(K02GroundTruthSteelKg);
        details.MaterialSchedule.Totals!.SteelKg.Should().Be(K02GroundTruthSteelKg);
    }

    private static GroupTechnicalDocumentationPipelineRunner CreateRunner(
        params ITechnicalDocumentationPipelineAgent[] agents)
    {
        IOptions<TechnicalDocumentationOptions> options = Options.Create(new TechnicalDocumentationOptions
        {
            EnableTestValidation = false,
            UseGroupPipeline = true,
        });

        return new GroupTechnicalDocumentationPipelineRunner(
            agents,
            options,
            NullLogger<GroupTechnicalDocumentationPipelineRunner>.Instance);
    }

    private sealed class RecordingGroupAgent : ITechnicalDocumentationPipelineAgent
    {
        private readonly List<string>? executionOrder;

        public RecordingGroupAgent(string name, List<string>? executionOrder)
        {
            Name = name;
            this.executionOrder = executionOrder;
        }

        public string Name { get; }

        public Task<TechnicalDocumentationAgentResult> ExecuteAsync(
            TechnicalDocumentationAgentContext context,
            CancellationToken cancellationToken)
        {
            executionOrder?.Add(Name);

            return Task.FromResult(new TechnicalDocumentationAgentResult(
                Success: true,
                AgentName: Name,
                Summary: $"{Name} completed",
                Warnings: []));
        }
    }

    private sealed class K02ConsolidationStubAgent : ITechnicalDocumentationPipelineAgent
    {
        public string Name => TechnicalDocumentationPipelineAgentNames.Consolidation;

        public Task<TechnicalDocumentationAgentResult> ExecuteAsync(
            TechnicalDocumentationAgentContext context,
            CancellationToken cancellationToken)
        {
            context.ProjectModel = new ProjectModel
            {
                Project = new ProjectModelMetadata { Name = "Dom jednorodzinny" },
                Slab = new ProjectModelSlab
                {
                    CoverageDescription = "Strop nad parterem",
                    Concrete = "C25/30",
                    SteelBottomKg = K02GroundTruthSteelKg,
                    SteelTopKg = 604.73,
                },
            };

            return Task.FromResult(new TechnicalDocumentationAgentResult(
                Success: true,
                AgentName: Name,
                Summary: "K-02 consolidation stub",
                Warnings: []));
        }
    }

    private sealed class K02MaterialsCalculationStubAgent : ITechnicalDocumentationPipelineAgent
    {
        public string Name => TechnicalDocumentationPipelineAgentNames.MaterialsCalculation;

        public Task<TechnicalDocumentationAgentResult> ExecuteAsync(
            TechnicalDocumentationAgentContext context,
            CancellationToken cancellationToken)
        {
            context.ComputedMaterialSchedule = new MaterialSchedule
            {
                CalculatedAt = DateTime.UtcNow,
                Ceilings = new CeilingMaterials
                {
                    Steel =
                    [
                        new MaterialItem
                        {
                            Element = "Stal zbrojenia dolnego (K-02)",
                            GrossQuantity = K02GroundTruthSteelKg,
                            NetQuantity = K02GroundTruthSteelKg,
                            Unit = "kg",
                            SourceType = "read",
                            SourceDrawings = ["K-02"],
                        }
                    ]
                },
                Steel =
                [
                    new MaterialItem
                    {
                        Element = "Stal zbrojenia dolnego (K-02)",
                        GrossQuantity = K02GroundTruthSteelKg,
                        NetQuantity = K02GroundTruthSteelKg,
                        Unit = "kg",
                    }
                ],
            };

            return Task.FromResult(new TechnicalDocumentationAgentResult(
                Success: true,
                AgentName: Name,
                Summary: "K-02 materials stub",
                Warnings: []));
        }
    }

    private sealed class StubTokenUsageRecorder : ICompletionTokenUsageRecorder
    {
        public int TotalTokens { get; private set; }

        public void Record(int totalTokens)
        {
            TotalTokens += totalTokens;
        }

        public void Reset()
        {
            TotalTokens = 0;
        }
    }
}
