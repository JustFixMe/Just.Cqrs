namespace Just.Cqrs.Internal;

/// <summary>
/// Marker interface for static type checking. Should not be used directly.
/// </summary>
public interface IGenericHandler<TRequest, TResponse>
    where TRequest : notnull
{
    ValueTask<TResponse> Handle(TRequest request, CancellationToken cancellation);
}
