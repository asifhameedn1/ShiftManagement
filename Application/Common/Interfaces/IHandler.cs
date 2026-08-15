namespace Application.Common.Interfaces;

/// <summary>
/// Defines a simple asynchronous handler for a request of type <typeparamref name="TRequest"/>
/// that returns a response of type <typeparamref name="TResponse"/>.
/// </summary>
public interface IHandler<in TRequest, TResponse>
{
    Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken = default);
}
