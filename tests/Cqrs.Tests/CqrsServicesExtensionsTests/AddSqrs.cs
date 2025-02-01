using Microsoft.Extensions.DependencyInjection;

namespace Cqrs.Tests.CqrsServicesExtensionsTests;

public class AddCqrs
{
    [Fact]
    public void WhenCalled_ShouldRegisterDispatcherClasses()
    {
        // Given
        ServiceCollection services = new();

        // When
        services.AddCqrs();

        // Then
        services.ShouldContain(
            elementPredicate: descriptor => 
                descriptor.ServiceType == typeof(ICommandDispatcher)
                && descriptor.ImplementationType == typeof(CommandDispatcherImpl)
                && descriptor.Lifetime == ServiceLifetime.Transient,
            expectedCount: 1
        );

        services.ShouldContain(
            elementPredicate: descriptor => 
                descriptor.ServiceType == typeof(IQueryDispatcher)
                && descriptor.ImplementationType == typeof(QueryDispatcherImpl)
                && descriptor.Lifetime == ServiceLifetime.Transient,
            expectedCount: 1
        );
    }

    [Fact]
    public void WhenCalledMultipleTimes_ShouldRegisterDispatcherClassesOnce()
    {
        // Given
        ServiceCollection services = new();

        // When
        services.AddCqrs();
        services.AddCqrs();
        services.AddCqrs();
        services.AddCqrs();

        // Then
        services.ShouldContain(
            elementPredicate: descriptor => 
                descriptor.ServiceType == typeof(ICommandDispatcher)
                && descriptor.ImplementationType == typeof(CommandDispatcherImpl)
                && descriptor.Lifetime == ServiceLifetime.Transient,
            expectedCount: 1
        );

        services.ShouldContain(
            elementPredicate: descriptor => 
                descriptor.ServiceType == typeof(IQueryDispatcher)
                && descriptor.ImplementationType == typeof(QueryDispatcherImpl)
                && descriptor.Lifetime == ServiceLifetime.Transient,
            expectedCount: 1
        );
    }
}
