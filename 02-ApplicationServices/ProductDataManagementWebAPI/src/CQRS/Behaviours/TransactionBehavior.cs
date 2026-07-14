using CQRS.PostCommit;
using Entities.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace CQRS.Behaviours
{
    public class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
    {
        private readonly AppDbContext appDbContext;
        private readonly IPostCommitDispatcher postCommitDispatcher;
        private readonly ILogger<TransactionBehavior<TRequest, TResponse>> logger;

        public TransactionBehavior(
            AppDbContext appDbContext,
            IPostCommitDispatcher postCommitDispatcher,
            ILogger<TransactionBehavior<TRequest, TResponse>> logger)
        {
            this.appDbContext = appDbContext;
            this.postCommitDispatcher = postCommitDispatcher;
            this.logger = logger;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
        {
            if (request is IRequestCommand<TResponse>)
            {
                // If a transaction is already active (e.g. from an outer TransactionBehavior),
                // execute within the existing transaction instead of creating a nested one.
                if (appDbContext.Database.CurrentTransaction is not null)
                {
                    TResponse innerResponse = await next();
                    await appDbContext.SaveChangesAsync(ct);
                    return innerResponse;
                }

                IExecutionStrategy strategy = appDbContext.Database.CreateExecutionStrategy();

                TResponse response = await strategy.ExecuteAsync(async () =>
                {
                    await using IDbContextTransaction transaction = await appDbContext.Database.BeginTransactionAsync(ct);
                    TResponse innerResponse = await next();
                    await appDbContext.SaveChangesAsync(ct);
                    await transaction.CommitAsync(ct);

                    return innerResponse;
                });

                try
                {
                    await postCommitDispatcher.DispatchAsync(ct);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Post-commit dispatcher failed for {RequestType}.", typeof(TRequest).Name);
                }

                return response;
            }

            return await next(ct);
        }
    }
}
