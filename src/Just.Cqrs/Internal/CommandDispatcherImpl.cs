using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Just.Cqrs.Internal;

internal sealed class CommandDispatcherImpl(
    IServiceProvider services,
    [FromKeyedServices(MethodsCacheServiceKey.DispatchCommand)]IMethodsCache methodsCache
) : ICommandDispatcher
{
    [ExcludeFromCodeCoverage]
    public ValueTask<TCommandResult> Dispatch<TCommandResult>(object command, CancellationToken cancellationToken)
        => DispatchCommand<TCommandResult>(command, cancellationToken);

    public ValueTask<TCommandResult> Dispatch<TCommandResult>(IKnownCommand<TCommandResult> command, CancellationToken cancellationToken)
        => DispatchCommand<TCommandResult>(command, cancellationToken);

    private ValueTask<TCommandResult> DispatchCommand<TCommandResult>(object command, CancellationToken cancellationToken)
    {
        var commandType = command.GetType();

        var dispatchCommandMethod = methodsCache.GetOrAdd(commandType, static t => typeof(CommandDispatcherImpl)
            .GetMethod(nameof(DispatchCommandImpl), BindingFlags.Instance | BindingFlags.NonPublic)!
            .MakeGenericMethod(t, typeof(TCommandResult)));

        return (ValueTask<TCommandResult>)dispatchCommandMethod
            .Invoke(this, [command, cancellationToken])!;
    }

    private ValueTask<TCommandResult> DispatchCommandImpl<TCommand, TCommandResult>(
        TCommand command,
        CancellationToken cancellationToken)
        where TCommand : notnull
    {
        var handler = services.GetRequiredService<ICommandHandler<TCommand, TCommandResult>>();
        var pipeline = services.GetServices<IDispatchBehaviour<TCommand, TCommandResult>>();
        using var pipelineEnumerator = pipeline.GetEnumerator();

        return DispatchDelegateFactory(pipelineEnumerator).Invoke();

        DispatchFurtherDelegate<TCommandResult> DispatchDelegateFactory(IEnumerator<IDispatchBehaviour<TCommand, TCommandResult>> enumerator) =>
            enumerator.MoveNext()
            ? (() => enumerator.Current.Handle(command, DispatchDelegateFactory(enumerator), cancellationToken))
            : (() => handler.Handle(command, cancellationToken));
    }
}
