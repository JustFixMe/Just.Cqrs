using System.Diagnostics.CodeAnalysis;
using Just.Cqrs.Internal;

namespace Just.Cqrs;

public interface ICommandHandler<TCommand, TCommandResult> : ICommandHandlerImpl, IGenericHandler<TCommand, TCommandResult>
    where TCommand : notnull
{
    new ValueTask<TCommandResult> Handle(TCommand command, CancellationToken cancellation);

    [ExcludeFromCodeCoverage]
    ValueTask<TCommandResult> IGenericHandler<TCommand, TCommandResult>.Handle(
        TCommand request,
        CancellationToken cancellationToken) => Handle(request, cancellationToken);
}
