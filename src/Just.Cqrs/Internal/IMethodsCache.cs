using System.Collections.Concurrent;
using System.Reflection;

namespace Just.Cqrs.Internal;

internal interface IMethodsCache
{
    MethodInfo GetOrAdd(Type key, Func<Type, MethodInfo> valueFactory);
}

internal static class MethodsCacheServiceKey
{
    internal const string DispatchQuery = "q";
    internal const string DispatchCommand = "c";
}

internal sealed class ConcurrentMethodsCache : ConcurrentDictionary<Type, MethodInfo>, IMethodsCache;
