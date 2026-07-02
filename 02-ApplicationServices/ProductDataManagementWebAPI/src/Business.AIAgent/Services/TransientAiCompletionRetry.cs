using System.ClientModel;

namespace Business.AIAgent.Services;

internal static class TransientAiCompletionRetry
{
    private const int MaxAttempts = 4;

    public static async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken)
    {
        Exception? lastException = null;

        for (int attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                return await action(cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                lastException = ex;

                if (attempt >= MaxAttempts || !IsTransient(ex))
                {
                    throw;
                }

                int delayMs = (int)Math.Pow(2, attempt) * 1000;
                await Task.Delay(delayMs, cancellationToken);
            }
        }

        throw lastException ?? new InvalidOperationException("AI completion retry failed without exception.");
    }

    private static bool IsTransient(Exception ex)
    {
        Exception? current = ex;
        while (current is not null)
        {
            if (current is ClientResultException clientResult)
            {
                int status = clientResult.Status;
                return status is 429 or 500 or 502 or 503;
            }

            current = current.InnerException;
        }

        return ex is HttpRequestException;
    }
}
