using Microsoft.Extensions.Logging;

namespace CQRS.PostCommit;

public sealed class PostCommitDispatcher : IPostCommitDispatcher
{
    private readonly List<Func<CancellationToken, Task>> actions = new List<Func<CancellationToken, Task>>();
    private readonly ILogger<PostCommitDispatcher> logger;

    public PostCommitDispatcher(ILogger<PostCommitDispatcher> logger)
    {
        this.logger = logger;
    }

    public void Enqueue(Func<CancellationToken, Task> action)
    {
        if (action is null)
        {
            return;
        }

        actions.Add(action);
    }

    public async Task DispatchAsync(CancellationToken cancellationToken)
    {
        if (actions.Count == 0)
        {
            return;
        }

        List<Func<CancellationToken, Task>> snapshot = new List<Func<CancellationToken, Task>>(actions);
        actions.Clear();

        foreach (Func<CancellationToken, Task> action in snapshot)
        {
            try
            {
                await action(cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Post-commit action failed; continuing with remaining actions.");
            }
        }
    }
}
