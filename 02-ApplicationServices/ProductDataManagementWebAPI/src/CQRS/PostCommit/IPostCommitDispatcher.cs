namespace CQRS.PostCommit;

/// <summary>
/// Kolejkuje akcje (np. broadcast SignalR) wykonywane DOPIERO po
/// pomyślnym commicie transakcji bieżącego requestu MediatR.
/// Scoped lifetime — instancja per request.
/// </summary>
public interface IPostCommitDispatcher
{
    void Enqueue(Func<CancellationToken, Task> action);

    Task DispatchAsync(CancellationToken cancellationToken);
}
