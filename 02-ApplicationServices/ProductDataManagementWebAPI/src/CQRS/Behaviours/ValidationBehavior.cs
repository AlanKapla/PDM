using Business.Interfaces.Exceptions;
using FluentValidation;
using MediatR;

namespace CQRS.Behaviours
{
    public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
    {
        private readonly IEnumerable<IValidator<TRequest>> _validators;

        public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
            => _validators = validators;

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
        {
            if (!_validators.Any())
            {
                return await next(ct);
            }

            var context = new ValidationContext<TRequest>(request);

            var failures = (await Task.WhenAll(
                    _validators.Select(v => v.ValidateAsync(context, ct))))
                .SelectMany(r => r.Errors)
                .Where(f => f is not null)
                .ToList();

            if (failures.Count != 0)
            {
                IEnumerable<string> errors = failures.Select(x => $"Property name: {x.PropertyName}, Error: {x.ErrorMessage}, Severity: {x.Severity}");

                string errorsStr = string.Join(", ", errors);

                throw new ValidationApiException(errorsStr);
            };

            return await next(ct);
        }
    }
}