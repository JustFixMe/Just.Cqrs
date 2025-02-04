using System.Diagnostics.CodeAnalysis;
using Just.Cqrs.Internal;

namespace Just.Cqrs;

public interface IQueryHandler<TQuery, TQueryResult> : IQueryHandlerImpl, IGenericHandler<TQuery, TQueryResult>
    where TQuery : notnull
{
    new ValueTask<TQueryResult> Handle(TQuery query, CancellationToken cancellation);

    [ExcludeFromCodeCoverage]
    ValueTask<TQueryResult> IGenericHandler<TQuery, TQueryResult>.Handle(
        TQuery request,
        CancellationToken cancellationToken) => Handle(request, cancellationToken);
}
