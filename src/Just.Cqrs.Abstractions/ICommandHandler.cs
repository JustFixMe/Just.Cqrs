using Just.Cqrs.Internal;

namespace Just.Cqrs;

public interface ICommandHandler<TCommand, TCommandResult> : ICommandHandlerImpl
    where TCommand : notnull
{
    ValueTask<TCommandResult> Handle(TCommand command, CancellationToken cancellation);
}
