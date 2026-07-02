using Business.AIAgent.Services;
using FluentAssertions;

namespace Business.Tests.Services.TechnicalDocumentation;

public sealed class TransientAiCompletionRetryTests
{
    [Fact]
    public async Task ExecuteAsync_retriesTransientFailures_beforeSucceeding()
    {
        int attempts = 0;

        string result = await TransientAiCompletionRetry.ExecuteAsync(
            _ =>
            {
                attempts++;
                if (attempts < 3)
                {
                    throw new HttpRequestException("503 Service Unavailable");
                }

                return Task.FromResult("ok");
            },
            CancellationToken.None);

        result.Should().Be("ok");
        attempts.Should().Be(3);
    }

    [Fact]
    public async Task ExecuteAsync_doesNotRetryCancellation()
    {
        using CancellationTokenSource cancellationTokenSource = new();
        cancellationTokenSource.Cancel();

        Func<Task> act = async () => await TransientAiCompletionRetry.ExecuteAsync<string>(
            _ => Task.FromResult("ok"),
            cancellationTokenSource.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
