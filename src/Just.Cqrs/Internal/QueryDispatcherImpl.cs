using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Just.Cqrs.Internal;

internal sealed class QueryDispatcherImpl(
    IServiceProvider services,
    [FromKeyedServices(MethodsCacheServiceKey.DispatchQuery)]IMethodsCache methodsCache
) : IQueryDispatcher
{
    [ExcludeFromCodeCoverage]
    public ValueTask<TQueryResult> Dispatch<TQueryResult>(object query, CancellationToken cancellationToken)
        => DispatchQuery<TQueryResult>(query, cancellationToken);

    public ValueTask<TQueryResult> Dispatch<TQueryResult>(IKnownQuery<TQueryResult> query, CancellationToken cancellationToken)
        => DispatchQuery<TQueryResult>(query, cancellationToken);

    private ValueTask<TQueryResult> DispatchQuery<TQueryResult>(object query, CancellationToken cancellationToken)
    {
        var queryType = query.GetType();

        var dispatchQueryMethod = methodsCache.GetOrAdd(queryType, static t => typeof(QueryDispatcherImpl)
            .GetMethod(nameof(DispatchQueryImpl), BindingFlags.Instance | BindingFlags.NonPublic)!
            .MakeGenericMethod(t, typeof(TQueryResult)));

        return (ValueTask<TQueryResult>)dispatchQueryMethod
            .Invoke(this, [query, cancellationToken])!;
    }

    private ValueTask<TQueryResult> DispatchQueryImpl<TQuery, TQueryResult>(
        TQuery query,
        CancellationToken cancellationToken)
        where TQuery : notnull
    {
        var handler = services.GetRequiredService<IQueryHandler<TQuery, TQueryResult>>();
        var pipeline = services.GetServices<IDispatchBehaviour<TQuery, TQueryResult>>();
        using var pipelineEnumerator = pipeline.GetEnumerator();

        return DispatchDelegateFactory(pipelineEnumerator).Invoke();

        DispatchFurtherDelegate<TQueryResult> DispatchDelegateFactory(IEnumerator<IDispatchBehaviour<TQuery, TQueryResult>> enumerator) =>
            enumerator.MoveNext()
            ? (() => enumerator.Current.Handle(query, DispatchDelegateFactory(enumerator), cancellationToken))
            : (() => handler.Handle(query, cancellationToken));
    }
}
