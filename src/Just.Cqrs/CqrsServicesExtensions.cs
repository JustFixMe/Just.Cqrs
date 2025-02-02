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
        foreach (var (service, impl, lifetime) in options.Behaviors)
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

    public static CqrsServicesOptions AddOpenBehavior(this CqrsServicesOptions options, Type Behavior, ServiceLifetime lifetime = ServiceLifetime.Singleton)
    {
        var interfaces = Behavior.FindInterfaces(
            static (x, t) => x.IsGenericType && x.GetGenericTypeDefinition() == (Type)t!,
            typeof(IDispatchBehavior<,>));

        if (interfaces.Length == 0)
        {
            throw new ArgumentException("Supplied type does not implement IDispatchBehavior<,> interface.", nameof(Behavior));
        }

        if (!Behavior.ContainsGenericParameters)
        {
            throw new ArgumentException("Supplied type is not sutable for open Behavior.", nameof(Behavior));
        }

        options.Behaviors.Add((typeof(IDispatchBehavior<,>), Behavior, lifetime));
        return options;
    }

    public static CqrsServicesOptions AddBehavior<TBehavior>(this CqrsServicesOptions options, ServiceLifetime lifetime = ServiceLifetime.Singleton)
        where TBehavior : notnull, IDispatchBehavior
    {
        var type = typeof(TBehavior);

        var interfaces = type.FindInterfaces(
            static (x, t) => x.IsGenericType && x.GetGenericTypeDefinition() == (Type)t!,
            typeof(IDispatchBehavior<,>));

        if (interfaces.Length == 0)
        {
            throw new InvalidOperationException("Supplied type does not implement IDispatchBehavior<,> interface.");
        }

        foreach (var interfaceType in interfaces)
        {
            options.Behaviors.Add((
                interfaceType,
                type,
                lifetime));
        }
        return options;
    }
}
