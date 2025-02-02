using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace Cqrs.Tests.CqrsServicesExtensionsTests;

public class AddOpenBehavior
{
    [ExcludeFromCodeCoverage]
    public class TestOpenBehavior<TRequest, TResponse> : IDispatchBehavior<TRequest, TResponse>
        where TRequest: notnull
    {
        public ValueTask<TResponse> Handle(TRequest request, DispatchFurtherDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }

    [Theory]
    [InlineData(ServiceLifetime.Transient)]
    [InlineData(ServiceLifetime.Scoped)]
    [InlineData(ServiceLifetime.Singleton)]
    public void WhenCalled_ShouldRegisterOpenDispatchBehavior(ServiceLifetime lifetime)
    {
        // Given
        ServiceCollection services = new();

        // When
        services.AddCqrs(opt => opt
            .AddOpenBehavior(typeof(TestOpenBehavior<,>), lifetime));

        // Then
        services.ShouldContain(
            elementPredicate: descriptor => 
                descriptor.ServiceType == typeof(IDispatchBehavior<,>)
                && descriptor.ImplementationType == typeof(TestOpenBehavior<,>)
                && descriptor.Lifetime == lifetime,
            expectedCount: 1
        );
    }

    [ExcludeFromCodeCoverage]
    public class InvalidOpenBehavior : IDispatchBehavior
    {
        public Type RequestType => throw new NotImplementedException();

        public Type ResponseType => throw new NotImplementedException();
    }

    [Fact]
    public void WhenCalledWithInvalidType_ShouldThrow()
    {
        // Given
        ServiceCollection services = new();

        // When
        var invalidOpenDispatchBehaviorType = typeof(InvalidOpenBehavior);

        // Then
        Should.Throw<ArgumentException>(() => services.AddCqrs(opt => opt
            .AddOpenBehavior(invalidOpenDispatchBehaviorType))
        );
    }

    public class TestCommand {}
    public class TestCommandResult {}
    [ExcludeFromCodeCoverage]
    public class NonGenericTestOpenBehavior : IDispatchBehavior<TestCommand, TestCommandResult>
    {
        public ValueTask<TestCommandResult> Handle(TestCommand request, DispatchFurtherDelegate<TestCommandResult> next, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }

    [Fact]
    public void WhenCalledWithNonGenericType_ShouldThrow()
    {
        // Given
        ServiceCollection services = new();

        // When
        var nonGenericOpenDispatchBehaviorType = typeof(NonGenericTestOpenBehavior);

        // Then
        Should.Throw<ArgumentException>(() => services.AddCqrs(opt => opt
            .AddOpenBehavior(nonGenericOpenDispatchBehaviorType))
        );
    }
}
