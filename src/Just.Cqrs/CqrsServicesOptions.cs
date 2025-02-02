using Microsoft.Extensions.DependencyInjection;

namespace Just.Cqrs;

public sealed class CqrsServicesOptions(IServiceCollection services)
{
    internal readonly List<(Type Service, Type Impl, ServiceLifetime Lifetime)> Behaviors = [];
    internal readonly List<(Type Service, Type Impl, ServiceLifetime Lifetime)> CommandHandlers = [];
    internal readonly List<(Type Service, Type Impl, ServiceLifetime Lifetime)> QueryHandlers = [];

    public IServiceCollection Services { get; } = services;
}
