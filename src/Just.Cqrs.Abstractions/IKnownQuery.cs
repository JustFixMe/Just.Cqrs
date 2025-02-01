namespace Just.Cqrs;

/// <summary>
/// Marker interface for Query type
/// </summary>
/// <typeparam name="TResult">Result of dispatching this query</typeparam>
public interface IKnownQuery<TResult>{}
