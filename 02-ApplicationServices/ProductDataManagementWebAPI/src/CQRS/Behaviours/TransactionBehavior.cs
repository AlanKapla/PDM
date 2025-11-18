using MediatR;
using Microsoft.EntityFrameworkCore;
using Entities.Context;

namespace CQRS.Behaviours
{
    public class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
    {
        private readonly AppDbContext appDbContext;

        public TransactionBehavior(AppDbContext appDbContext)
        {
            this.appDbContext = appDbContext;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
        {
            if (request is IRequestCommand<TResponse>)
            {
                var strategy = appDbContext.Database.CreateExecutionStrategy();

                return await strategy.ExecuteAsync(async () =>
                {
                    await using var transaction = await appDbContext.Database.BeginTransactionAsync(ct);
                    var response = await next();
                    await appDbContext.SaveChangesAsync(ct);
                    await transaction.CommitAsync(ct);

                    return response;
                });
            }

            return await next(ct);
        }
    }
}
