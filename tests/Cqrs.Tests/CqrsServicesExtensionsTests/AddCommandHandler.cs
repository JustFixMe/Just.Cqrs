using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace Cqrs.Tests.CqrsServicesExtensionsTests;

public class AddCommandHandler
{
    public class TestCommand {}
    public class TestCommandResult {}
    [ExcludeFromCodeCoverage]
    public class TestCommandHandler : ICommandHandler<TestCommand, TestCommandResult>
    {
        public ValueTask<TestCommandResult> Handle(TestCommand command, CancellationToken cancellation)
        {
            throw new NotImplementedException();
        }
    }

    [Theory]
    [InlineData(ServiceLifetime.Transient)]
    [InlineData(ServiceLifetime.Scoped)]
    [InlineData(ServiceLifetime.Singleton)]
    public void WhenCalled_ShouldRegisterCommandHandler(ServiceLifetime lifetime)
    {
        // Given
        ServiceCollection services = new();

        // When
        services.AddCqrs(opt => opt.AddCommandHandler<TestCommandHandler>(lifetime));

        // Then
        services.ShouldContain(
            elementPredicate: descriptor => 
                descriptor.ServiceType == typeof(ICommandHandler<TestCommand, TestCommandResult>)
                && descriptor.ImplementationType == typeof(TestCommandHandler)
                && descriptor.Lifetime == lifetime,
            expectedCount: 1
        );
    }

    [Fact]
    public void WhenCalledMultipleTimes_ShouldRegisterCommandHandlerOnce()
    {
        // Given
        ServiceCollection services = new();

        // When
        services.AddCqrs(opt => opt
            .AddCommandHandler<TestCommandHandler>()
            .AddCommandHandler<TestCommandHandler>()
            .AddCommandHandler<TestCommandHandler>());

        services.AddCqrs(opt => opt
            .AddCommandHandler<TestCommandHandler>());

        // Then
        services.ShouldContain(
            elementPredicate: descriptor => 
                descriptor.ServiceType == typeof(ICommandHandler<TestCommand, TestCommandResult>)
                && descriptor.ImplementationType == typeof(TestCommandHandler)
                && descriptor.Lifetime == ServiceLifetime.Transient,
            expectedCount: 1
        );
    }

    public class SecondTestCommand {}
    public class SecondTestCommandResult {}
    [ExcludeFromCodeCoverage]
    public class SecondTestCommandHandler : ICommandHandler<SecondTestCommand, SecondTestCommandResult>
    {
        public ValueTask<SecondTestCommandResult> Handle(SecondTestCommand command, CancellationToken cancellation)
        {
            throw new NotImplementedException();
        }
    }

    public class ThirdTestCommand {}
    public class ThirdTestCommandResult {}
    [ExcludeFromCodeCoverage]
    public class ThirdTestCommandHandler : ICommandHandler<ThirdTestCommand, ThirdTestCommandResult>
    {
        public ValueTask<ThirdTestCommandResult> Handle(ThirdTestCommand command, CancellationToken cancellation)
        {
            throw new NotImplementedException();
        }
    }

    [Fact]
    public void WhenCalledMultipleTimes_ShouldRegisterAllCommandHandlers()
    {
        // Given
        ServiceCollection services = new();

        // When
        services.AddCqrs(opt => opt
            .AddCommandHandler<TestCommandHandler>()
            .AddCommandHandler<SecondTestCommandHandler>());
        services.AddCqrs(opt => opt
            .AddCommandHandler<ThirdTestCommandHandler>());

        // Then
        services.ShouldContain(
            elementPredicate: descriptor => 
                descriptor.ServiceType == typeof(ICommandHandler<TestCommand, TestCommandResult>)
                && descriptor.ImplementationType == typeof(TestCommandHandler)
                && descriptor.Lifetime == ServiceLifetime.Transient,
            expectedCount: 1
        );
        services.ShouldContain(
            elementPredicate: descriptor => 
                descriptor.ServiceType == typeof(ICommandHandler<SecondTestCommand, SecondTestCommandResult>)
                && descriptor.ImplementationType == typeof(SecondTestCommandHandler)
                && descriptor.Lifetime == ServiceLifetime.Transient,
            expectedCount: 1
        );
        services.ShouldContain(
            elementPredicate: descriptor => 
                descriptor.ServiceType == typeof(ICommandHandler<ThirdTestCommand, ThirdTestCommandResult>)
                && descriptor.ImplementationType == typeof(ThirdTestCommandHandler)
                && descriptor.Lifetime == ServiceLifetime.Transient,
            expectedCount: 1
        );
    }
}
