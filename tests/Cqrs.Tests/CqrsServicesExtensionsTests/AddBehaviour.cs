using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace Cqrs.Tests.CqrsServicesExtensionsTests;

public class AddBehaviour
{
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

    [Theory]
    [InlineData(ServiceLifetime.Transient)]
    [InlineData(ServiceLifetime.Scoped)]
    [InlineData(ServiceLifetime.Singleton)]
    public void WhenCalled_ShouldRegisterDispatchBehaviour(ServiceLifetime lifetime)
    {
        // Given
        ServiceCollection services = new();

        // When
        services.AddCqrs(opt => opt
            .AddBehaviour<NonGenericTestOpenBehaviour>(lifetime));

        // Then
        services.ShouldContain(
            elementPredicate: descriptor => 
                descriptor.ServiceType == typeof(IDispatchBehaviour<TestCommand, TestCommandResult>)
                && descriptor.ImplementationType == typeof(NonGenericTestOpenBehaviour)
                && descriptor.Lifetime == lifetime,
            expectedCount: 1
        );
    }

    [ExcludeFromCodeCoverage]
    public class InvalidTestBehaviour : IDispatchBehaviour
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

        // Then
        Should.Throw<InvalidOperationException>(() => services.AddCqrs(opt => opt
            .AddBehaviour<InvalidTestBehaviour>())
        );
    }
}
