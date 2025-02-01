namespace Just.Cqrs;

public interface ICommandDispatcher
{
    ValueTask<TCommandResult> Dispatch<TCommandResult>(
        object command,
        CancellationToken cancellationToken);
    ValueTask<TCommandResult> Dispatch<TCommandResult>(
        IKnownCommand<TCommandResult> command,
        CancellationToken cancellationToken);
}
