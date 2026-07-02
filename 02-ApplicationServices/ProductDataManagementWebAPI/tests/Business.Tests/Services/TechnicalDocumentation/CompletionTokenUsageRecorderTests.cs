using Business.AIAgent.Services;
using FluentAssertions;

namespace Business.Tests.Services.TechnicalDocumentation;

public sealed class CompletionTokenUsageRecorderTests
{
    [Fact]
    public void Record_accumulatesTotalTokens()
    {
        // Arrange
        ScopedCompletionTokenUsageRecorder recorder = new();

        // Act
        recorder.Record(120);
        recorder.Record(80);

        // Assert
        recorder.TotalTokens.Should().Be(200);
    }

    [Fact]
    public void Reset_clearsAccumulatedTokens()
    {
        // Arrange
        ScopedCompletionTokenUsageRecorder recorder = new();
        recorder.Record(500);

        // Act
        recorder.Reset();

        // Assert
        recorder.TotalTokens.Should().Be(0);
    }

    [Fact]
    public void Record_ignoresNonPositiveValues()
    {
        // Arrange
        ScopedCompletionTokenUsageRecorder recorder = new();

        // Act
        recorder.Record(0);
        recorder.Record(-10);
        recorder.Record(25);

        // Assert
        recorder.TotalTokens.Should().Be(25);
    }

    [Fact]
    public void Record_isThreadSafe()
    {
        // Arrange
        ScopedCompletionTokenUsageRecorder recorder = new();
        const int threads = 8;
        const int tokensPerThread = 100;

        // Act
        Parallel.For(0, threads, _ => recorder.Record(tokensPerThread));

        // Assert
        recorder.TotalTokens.Should().Be(threads * tokensPerThread);
    }
}
