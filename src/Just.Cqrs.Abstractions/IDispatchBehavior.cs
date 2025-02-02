using System.Diagnostics.CodeAnalysis;

namespace Just.Cqrs;

/// <summary>
/// Delegate representing the rest of the pipeline
/// </summary>
/// <typeparam name="TResponse">Result type of dispatching command/query</typeparam>
/// <returns>Result of executing the rest of the pipeline</returns>
public delegate ValueTask<TResponse> DispatchFurtherDelegate<TResponse>();

/// <summary>
/// Marker interface for static type checking. Should not be used directly.
/// </summary>
public interface IDispatchBehavior
{
    Type RequestType { get; }
    Type ResponseType { get; }
}

/// <summary>
/// Middleware analog for dispatching commands/queries
/// </summary>
/// <typeparam name="TRequest">Request type</typeparam>
/// <typeparam name="TResponse">Result type of dispatching command/query</typeparam>
public interface IDispatchBehavior<in TRequest, TResponse> : IDispatchBehavior
    where TRequest : notnull
{
    ValueTask<TResponse> Handle(
        TRequest request,
        DispatchFurtherDelegate<TResponse> next,
        CancellationToken cancellationToken);

    [ExcludeFromCodeCoverage]
    Type IDispatchBehavior.RequestType => typeof(TRequest);
    [ExcludeFromCodeCoverage]
    Type IDispatchBehavior.ResponseType => typeof(TResponse);
}
