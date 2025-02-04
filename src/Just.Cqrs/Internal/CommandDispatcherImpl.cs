using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Just.Cqrs.Internal;

internal sealed class CommandDispatcherImpl(
    IServiceProvider services,
    IMethodsCache methodsCache
) : DispatcherBase(methodsCache), ICommandDispatcher
{
    private static readonly Func<(Type RequestType, Type ResponseType), Delegate> CreateDispatchCommandDelegate;
    static CommandDispatcherImpl()
    {
        var dispatcherType = typeof(CommandDispatcherImpl);
        var genericDispatchImplMethod = dispatcherType
            .GetMethod(nameof(DispatchCommandImpl), BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"{nameof(DispatchCommandImpl)} method not found.");
        CreateDispatchCommandDelegate = methodsCacheKey => CreateDispatchDelegate(methodsCacheKey, dispatcherType, genericDispatchImplMethod);
    }

    [ExcludeFromCodeCoverage]
    public ValueTask<TCommandResult> Dispatch<TCommandResult>(object command, CancellationToken cancellationToken)
        => DispatchInternal<TCommandResult>(CreateDispatchCommandDelegate, command, cancellationToken);

    public ValueTask<TCommandResult> Dispatch<TCommandResult>(IKnownCommand<TCommandResult> command, CancellationToken cancellationToken)
        => DispatchInternal<TCommandResult>(CreateDispatchCommandDelegate, command, cancellationToken);

    private ValueTask<TCommandResult> DispatchCommandImpl<TCommand, TCommandResult>(
        TCommand command,
        CancellationToken cancellationToken)
        where TCommand : notnull
    {
        var handler = services.GetRequiredService<ICommandHandler<TCommand, TCommandResult>>();
        var pipeline = services.GetServices<IDispatchBehavior<TCommand, TCommandResult>>();

        return DispatchDelegateFactory(command, handler, pipeline, cancellationToken).Invoke();
    }
}
