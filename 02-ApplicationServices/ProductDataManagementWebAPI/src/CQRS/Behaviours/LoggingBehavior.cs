using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace CQRS.Behaviours
{
    public class LoggingBehavior<TRequest, TResponse>
        : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        private readonly ILogger<LoggingBehavior<TRequest, TResponse>> logger;

        public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
        {
            this.logger = logger;
        }

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken ct)
        {
            string requestName = typeof(TRequest).Name;

            logger.LogInformation("Handling {RequestName}", requestName);

            Stopwatch stopwatch = Stopwatch.StartNew();

            try
            {
                TResponse response = await next();
                stopwatch.Stop();

                logger.LogInformation(
                    "Handled {RequestName} in {ElapsedMs}ms",
                    requestName, stopwatch.ElapsedMilliseconds);

                return response;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                logger.LogError(ex,
                    "Error handling {RequestName} after {ElapsedMs}ms",
                    requestName, stopwatch.ElapsedMilliseconds);

                throw;
            }
        }
    }
}
