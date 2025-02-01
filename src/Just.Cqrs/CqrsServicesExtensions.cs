using Just.Cqrs;
using Just.Cqrs.Internal;
using Microsoft.Extensions.DependencyInjection.Extensions;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Microsoft.Extensions.DependencyInjection;
#pragma warning restore IDE0130

public static class CqrsServicesExtensions
{
    public static IServiceCollection AddCqrs(this IServiceCollection services, Action<CqrsServicesOptions>? configure = null)
    {
        var options = new CqrsServicesOptions(services);
        configure?.Invoke(options);

        services.TryAddKeyedSingleton<IMethodsCache, ConcurrentMethodsCache>(MethodsCacheServiceKey.DispatchCommand);
        services.TryAddTransient<ICommandDispatcher, CommandDispatcherImpl>();

        services.TryAddKeyedSingleton<IMethodsCache, ConcurrentMethodsCache>(MethodsCacheServiceKey.DispatchQuery);
        services.TryAddTransient<IQueryDispatcher, QueryDispatcherImpl>();

        foreach (var (service, impl, lifetime) in options.CommandHandlers)
        {
            services.TryAdd(new ServiceDescriptor(service, impl, lifetime));
        }
        foreach (var (service, impl, lifetime) in options.QueryHandlers)
        {
            services.TryAdd(new ServiceDescriptor(service, impl, lifetime));
        }
        foreach (var (service, impl, lifetime) in options.Behaviours)
        {
            services.Add(new ServiceDescriptor(service, impl, lifetime));
        }

        return services;
    }

    public static CqrsServicesOptions AddCommandHandler<TCommandHandler>(
        this CqrsServicesOptions options,
        ServiceLifetime lifetime = ServiceLifetime.Transient)
        where TCommandHandler : notnull, ICommandHandlerImpl
    {
        var type = typeof(TCommandHandler);
        var handlerInterfaces = type.FindInterfaces(
            static (x, t) => x.IsGenericType && x.GetGenericTypeDefinition() == (Type)t!,
            typeof(ICommandHandler<,>));

        foreach (var interfaceType in handlerInterfaces)
        {
            options.CommandHandlers.Add((
                interfaceType,
                type,
                lifetime));
        }
        return options;
    }

    public static CqrsServicesOptions AddQueryHandler<TQueryHandler>(
        this CqrsServicesOptions options,
        ServiceLifetime lifetime = ServiceLifetime.Transient)
        where TQueryHandler : notnull, IQueryHandlerImpl
    {
        var type = typeof(TQueryHandler);
        var handlerInterfaces = type.FindInterfaces(
            static (x, t) => x.IsGenericType && x.GetGenericTypeDefinition() == (Type)t!,
            typeof(IQueryHandler<,>));

        foreach (var interfaceType in handlerInterfaces)
        {
            options.QueryHandlers.Add((
                interfaceType,
                type,
                lifetime));
        }
        return options;
    }

    public static CqrsServicesOptions AddOpenBehaviour(this CqrsServicesOptions options, Type behaviour, ServiceLifetime lifetime = ServiceLifetime.Singleton)
    {
        var interfaces = behaviour.FindInterfaces(
            static (x, t) => x.IsGenericType && x.GetGenericTypeDefinition() == (Type)t!,
            typeof(IDispatchBehaviour<,>));

        if (interfaces.Length == 0)
        {
            throw new ArgumentException("Supplied type does not implement IDispatchBehaviour<,> interface.", nameof(behaviour));
        }

        if (!behaviour.ContainsGenericParameters)
        {
            throw new ArgumentException("Supplied type is not sutable for open behaviour.", nameof(behaviour));
        }

        options.Behaviours.Add((typeof(IDispatchBehaviour<,>), behaviour, lifetime));
        return options;
    }

    public static CqrsServicesOptions AddBehaviour<TBehaviour>(this CqrsServicesOptions options, ServiceLifetime lifetime = ServiceLifetime.Singleton)
        where TBehaviour : notnull, IDispatchBehaviour
    {
        var type = typeof(TBehaviour);

        var interfaces = type.FindInterfaces(
            static (x, t) => x.IsGenericType && x.GetGenericTypeDefinition() == (Type)t!,
            typeof(IDispatchBehaviour<,>));

        if (interfaces.Length == 0)
        {
            throw new InvalidOperationException("Supplied type does not implement IDispatchBehaviour<,> interface.");
        }

        foreach (var interfaceType in interfaces)
        {
            options.Behaviours.Add((
                interfaceType,
                type,
                lifetime));
        }
        return options;
    }
}
