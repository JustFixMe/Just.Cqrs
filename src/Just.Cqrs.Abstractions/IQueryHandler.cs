using Just.Cqrs.Internal;

namespace Just.Cqrs;

public interface IQueryHandler<TQuery, TQueryResult> : IQueryHandlerImpl
    where TQuery : notnull
{
    ValueTask<TQueryResult> Handle(TQuery query, CancellationToken cancellation);
}
