using System.Collections.Concurrent;

namespace Just.Cqrs.Internal;

internal interface IMethodsCache
{
    Delegate GetOrAdd((Type RequestType, Type ResponseType) key, Func<(Type RequestType, Type ResponseType), Delegate> valueFactory);
}

internal sealed class ConcurrentMethodsCache : ConcurrentDictionary<(Type RequestType, Type ResponseType), Delegate>, IMethodsCache;
