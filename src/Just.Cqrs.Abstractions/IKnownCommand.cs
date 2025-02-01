namespace Just.Cqrs;

/// <summary>
/// Marker interface for Command type
/// </summary>
/// <typeparam name="TResult">Result of dispatching this command</typeparam>
public interface IKnownCommand<TResult>{}
