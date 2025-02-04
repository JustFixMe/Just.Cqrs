using System.Linq.Expressions;
using System.Reflection;

namespace Just.Cqrs.Internal;

internal abstract class DispatcherBase(IMethodsCache methodsCache)
{
    protected IMethodsCache MethodsCache { get; } = methodsCache;
    protected ValueTask<TResult> DispatchInternal<TResult>(
        Func<(Type, Type), Delegate> delegateFactory,
        object request,
        CancellationToken cancellationToken)
    {
        var cacheKey = (request.GetType(), typeof(TResult));
        var dispatchDelegate = (Func<DispatcherBase, object, CancellationToken, ValueTask<TResult>>)
            MethodsCache.GetOrAdd(cacheKey, delegateFactory);
        return dispatchDelegate(this, request, cancellationToken);
    }

    protected DispatchFurtherDelegate<TResponse> DispatchDelegateFactory<TRequest, TResponse, THandler>(
        TRequest request,
        THandler handler,
        IEnumerable<IDispatchBehavior<TRequest, TResponse>> behaviors,
        CancellationToken cancellationToken)
        where TRequest : notnull
        where THandler : IGenericHandler<TRequest, TResponse>
    {
        DispatchFurtherDelegate<TResponse> pipeline = behaviors.Reverse()
            .Aggregate<IDispatchBehavior<TRequest, TResponse>, DispatchFurtherDelegate<TResponse>>(
                () => handler.Handle(request, cancellationToken),
                (next, behavior) => () => behavior.Handle(request, next, cancellationToken)
            );

        return pipeline;
    }

    internal static Delegate CreateDispatchDelegate((Type RequestType, Type ResponseType) methodsCacheKey, Type dispatcherType, MethodInfo genericDispatchImplMethod)
    {
        var dispatcherBaseType = typeof(DispatcherBase);
        var (requestType, responseType) = methodsCacheKey;
        var dispatchImplMethod = genericDispatchImplMethod.MakeGenericMethod(requestType, responseType);

        ParameterExpression[] lambdaParameters =
        [
            Expression.Parameter(dispatcherBaseType),
            Expression.Parameter(typeof(object)),
            Expression.Parameter(typeof(CancellationToken)),
        ];
        Expression[] callParameters =
        [
            Expression.Convert(lambdaParameters[1], requestType),
            lambdaParameters[2],
        ];
        var lambdaExpression = Expression.Lambda(
            typeof(Func<,,,>).MakeGenericType(
                dispatcherBaseType,
                typeof(object),
                typeof(CancellationToken),
                typeof(ValueTask<>).MakeGenericType(responseType)),
            Expression.Call(Expression.Convert(lambdaParameters[0], dispatcherType), dispatchImplMethod, callParameters),
            lambdaParameters
        );
        var dispatchQueryDelegate = lambdaExpression.Compile();

        return dispatchQueryDelegate;
    }
}
