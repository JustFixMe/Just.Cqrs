using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace Cqrs.Tests.CqrsServicesExtensionsTests;

public class AddOpenBehaviour
{
    [ExcludeFromCodeCoverage]
    public class TestOpenBehaviour<TRequest, TResponse> : IDispatchBehaviour<TRequest, TResponse>
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
    public void WhenCalled_ShouldRegisterOpenDispatchBehaviour(ServiceLifetime lifetime)
    {
        // Given
        ServiceCollection services = new();

        // When
        services.AddCqrs(opt => opt
            .AddOpenBehaviour(typeof(TestOpenBehaviour<,>), lifetime));

        // Then
        services.ShouldContain(
            elementPredicate: descriptor => 
                descriptor.ServiceType == typeof(IDispatchBehaviour<,>)
                && descriptor.ImplementationType == typeof(TestOpenBehaviour<,>)
                && descriptor.Lifetime == lifetime,
            expectedCount: 1
        );
    }

    [ExcludeFromCodeCoverage]
    public class InvalidOpenBehaviour : IDispatchBehaviour
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
        var invalidOpenDispatchBehaviourType = typeof(InvalidOpenBehaviour);

        // Then
        Should.Throw<ArgumentException>(() => services.AddCqrs(opt => opt
            .AddOpenBehaviour(invalidOpenDispatchBehaviourType))
        );
    }

    public class TestCommand {}
    public class TestCommandResult {}
    [ExcludeFromCodeCoverage]
    public class NonGenericTestOpenBehaviour : IDispatchBehaviour<TestCommand, TestCommandResult>
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
        var nonGenericOpenDispatchBehaviourType = typeof(NonGenericTestOpenBehaviour);

        // Then
        Should.Throw<ArgumentException>(() => services.AddCqrs(opt => opt
            .AddOpenBehaviour(nonGenericOpenDispatchBehaviourType))
        );
    }
}
