using Business.Implementation.Services.AI.TechnicalDocumentation.Pipeline;
using Business.Interfaces.Configurations;
using Business.Interfaces.Services;
using Business.Interfaces.Services.TechnicalDocumentation;
using Business.Interfaces.WebModels.TechnicalDocumentation;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Business.Tests.Services.TechnicalDocumentation;

public sealed class TechnicalDocumentationPipelineRunnerTests
{
    [Fact]
    public async Task RunAsync_runsCrossReferenceRoomsAndOpeningsInParallel()
    {
        RecordingPipelineAgent extraction = new(TechnicalDocumentationPipelineAgentNames.ImageExtraction);
        RecordingPipelineAgent crossReference = new(TechnicalDocumentationPipelineAgentNames.CrossReference, delayMs: 200);
        RecordingPipelineAgent rooms = new(TechnicalDocumentationPipelineAgentNames.Rooms, delayMs: 200);
        RecordingPipelineAgent openings = new(TechnicalDocumentationPipelineAgentNames.Openings, delayMs: 200);
        RecordingPipelineAgent materials = new(TechnicalDocumentationPipelineAgentNames.MaterialsCalculation);
        RecordingPipelineAgent report = new(TechnicalDocumentationPipelineAgentNames.Report);

        TechnicalDocumentationPipelineRunner runner = CreateRunner(
            extraction,
            crossReference,
            rooms,
            openings,
            materials,
            report);

        DateTimeOffset startedAt = DateTimeOffset.UtcNow;
        ProjectTechnicalDocumentationDetails details = await runner.RunAsync(
            [new TechnicalDocumentationImageInput([], "A-02.pdf", 1)],
            CancellationToken.None);
        TimeSpan elapsed = DateTimeOffset.UtcNow - startedAt;

        details.Should().NotBeNull();
        crossReference.StartedAt.Should().NotBeNull();
        rooms.StartedAt.Should().NotBeNull();
        openings.StartedAt.Should().NotBeNull();

        TimeSpan parallelSpread = new[]
        {
            crossReference.StartedAt!.Value,
            rooms.StartedAt!.Value,
            openings.StartedAt!.Value
        }.Max() - new[]
        {
            crossReference.StartedAt!.Value,
            rooms.StartedAt!.Value,
            openings.StartedAt!.Value
        }.Min();

        parallelSpread.Should().BeLessThan(TimeSpan.FromMilliseconds(150));
        elapsed.Should().BeLessThan(TimeSpan.FromMilliseconds(900));
    }

    [Fact]
    public async Task RunAsync_skipsDetailsValidation_whenTestValidationDisabled()
    {
        RecordingPipelineAgent validation = new(TechnicalDocumentationPipelineAgentNames.DetailsValidation);
        TechnicalDocumentationPipelineRunner runner = CreateRunner(
            new RecordingPipelineAgent(TechnicalDocumentationPipelineAgentNames.ImageExtraction),
            new RecordingPipelineAgent(TechnicalDocumentationPipelineAgentNames.CrossReference),
            new RecordingPipelineAgent(TechnicalDocumentationPipelineAgentNames.Rooms),
            new RecordingPipelineAgent(TechnicalDocumentationPipelineAgentNames.Openings),
            new RecordingPipelineAgent(TechnicalDocumentationPipelineAgentNames.MaterialsCalculation),
            new RecordingPipelineAgent(TechnicalDocumentationPipelineAgentNames.Report),
            validation);

        await runner.RunAsync(
            [new TechnicalDocumentationImageInput([], "A-02.pdf", 1)],
            CancellationToken.None);

        validation.ExecutionCount.Should().Be(0);
    }

    private static TechnicalDocumentationPipelineRunner CreateRunner(
        params RecordingPipelineAgent[] agents)
    {
        IOptions<TechnicalDocumentationOptions> options = Options.Create(new TechnicalDocumentationOptions
        {
            EnableTestValidation = false,
            UseGroupPipeline = false,
        });

        IEnumerable<ITechnicalDocumentationPipelineAgent> agentList =
            agents.Cast<ITechnicalDocumentationPipelineAgent>();

        LegacyTechnicalDocumentationPipelineRunner legacyRunner = new(
            agentList,
            options,
            NullLogger<LegacyTechnicalDocumentationPipelineRunner>.Instance);

        GroupTechnicalDocumentationPipelineRunner groupRunner = new(
            agentList,
            options,
            NullLogger<GroupTechnicalDocumentationPipelineRunner>.Instance);

        return new TechnicalDocumentationPipelineRunner(
            legacyRunner,
            groupRunner,
            options,
            NullLogger<TechnicalDocumentationPipelineRunner>.Instance);
    }

    private sealed class RecordingPipelineAgent : ITechnicalDocumentationPipelineAgent
    {
        private readonly int delayMs;

        public RecordingPipelineAgent(string name, int delayMs = 0)
        {
            Name = name;
            this.delayMs = delayMs;
        }

        public string Name { get; }

        public int ExecutionCount { get; private set; }

        public DateTimeOffset? StartedAt { get; private set; }

        public Task<TechnicalDocumentationAgentResult> ExecuteAsync(
            TechnicalDocumentationAgentContext context,
            CancellationToken cancellationToken)
        {
            ExecutionCount++;
            StartedAt = DateTimeOffset.UtcNow;

            if (delayMs > 0)
            {
                return Task.Run(async () =>
                {
                    await Task.Delay(delayMs, cancellationToken);
                    return CreateSuccessResult();
                }, cancellationToken);
            }

            return Task.FromResult(CreateSuccessResult());
        }

        private TechnicalDocumentationAgentResult CreateSuccessResult()
        {
            return new TechnicalDocumentationAgentResult(
                Success: true,
                AgentName: Name,
                Summary: $"{Name} completed",
                Warnings: []);
        }
    }
}
