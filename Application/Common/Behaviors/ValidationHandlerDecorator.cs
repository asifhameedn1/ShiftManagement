using FluentValidation;
using Domain.Common;
using Application.Common.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Common.Behaviors;

/// <summary>
/// A decorator for <see cref="IHandler{TRequest, TResponse}"/> that runs FluentValidation validators before the inner handler.
/// Returns a failure Result instead of throwing a ValidationException.
/// </summary>
public sealed class ValidationHandlerDecorator<TRequest, TResponse> : IHandler<TRequest, TResponse>
    where TResponse : Result
{
    private readonly IHandler<TRequest, TResponse> _inner;
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationHandlerDecorator(
        IHandler<TRequest, TResponse> inner,
        IServiceProvider serviceProvider)
    {
        _inner = inner;
        _validators = serviceProvider.GetServices<IValidator<TRequest>>();
    }

    public async Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken = default)
    {
        if (_validators.Any())
        {
            var context = new ValidationContext<TRequest>(request);

            var failures = _validators
                .Select(v => v.Validate(context))
                .SelectMany(r => r.Errors)
                .Where(f => f != null)
                .ToList();

            if (failures.Count > 0)
            {
                var error = Error.Validation(
                    typeof(TRequest).Name,
                    string.Join("; ", failures.Select(f => f.ErrorMessage)));

                // Safely construct a failure Result or Result<TValue> without InvalidCastException
                if (typeof(TResponse).IsGenericType && typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
                {
                    var valueType = typeof(TResponse).GetGenericArguments()[0];
                    var failureMethod = typeof(Result)
                        .GetMethods()
                        .First(m => m.Name == nameof(Result.Failure) && m.IsGenericMethod)
                        .MakeGenericMethod(valueType);
                    return (TResponse)failureMethod.Invoke(null, new object[] { error })!;
                }
                
                return (TResponse)(object)Result.Failure(error);
            }
        }

        return await _inner.HandleAsync(request, cancellationToken);
    }
}
