using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Just.Cqrs.Internal;

internal sealed class QueryDispatcherImpl(
    IServiceProvider services,
    IMethodsCache methodsCache
) : DispatcherBase(methodsCache), IQueryDispatcher
{
    private static readonly Func<(Type RequestType, Type ResponseType), Delegate> CreateDispatchQueryDelegate;
    static QueryDispatcherImpl()
    {
        var dispatcherType = typeof(QueryDispatcherImpl);
        var genericDispatchImplMethod = dispatcherType
            .GetMethod(nameof(DispatchQueryImpl), BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"{nameof(DispatchQueryImpl)} method not found.");
        CreateDispatchQueryDelegate = methodsCacheKey => CreateDispatchDelegate(methodsCacheKey, dispatcherType, genericDispatchImplMethod);
    }

    [ExcludeFromCodeCoverage]
    public ValueTask<TQueryResult> Dispatch<TQueryResult>(object query, CancellationToken cancellationToken)
        => DispatchInternal<TQueryResult>(CreateDispatchQueryDelegate, query, cancellationToken);

    public ValueTask<TQueryResult> Dispatch<TQueryResult>(IKnownQuery<TQueryResult> query, CancellationToken cancellationToken)
        => DispatchInternal<TQueryResult>(CreateDispatchQueryDelegate, query, cancellationToken);

    private ValueTask<TQueryResult> DispatchQueryImpl<TQuery, TQueryResult>(
        TQuery query,
        CancellationToken cancellationToken)
        where TQuery : notnull
    {
        var handler = services.GetRequiredService<IQueryHandler<TQuery, TQueryResult>>();
        var pipeline = services.GetServices<IDispatchBehavior<TQuery, TQueryResult>>();

        return DispatchDelegateFactory(query, handler, pipeline, cancellationToken).Invoke();
    }
}
