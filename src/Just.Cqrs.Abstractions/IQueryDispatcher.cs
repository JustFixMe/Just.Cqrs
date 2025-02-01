namespace Just.Cqrs;

public interface IQueryDispatcher
{
    ValueTask<TQueryResult> Dispatch<TQueryResult>(
        object query,
        CancellationToken cancellationToken);

    ValueTask<TQueryResult> Dispatch<TQueryResult>(
        IKnownQuery<TQueryResult> query,
        CancellationToken cancellationToken);
}
