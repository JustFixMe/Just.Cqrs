using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace Cqrs.Tests.CqrsServicesExtensionsTests;

public class AddQueryHandler
{
    public class TestQuery {}
    public class TestQueryResult {}
    [ExcludeFromCodeCoverage]
    public class TestQueryHandler : IQueryHandler<TestQuery, TestQueryResult>
    {
        public ValueTask<TestQueryResult> Handle(TestQuery Query, CancellationToken cancellation)
        {
            throw new NotImplementedException();
        }
    }

    [Theory]
    [InlineData(ServiceLifetime.Transient)]
    [InlineData(ServiceLifetime.Scoped)]
    [InlineData(ServiceLifetime.Singleton)]
    public void WhenCalled_ShouldRegisterQueryHandler(ServiceLifetime lifetime)
    {
        // Given
        ServiceCollection services = new();

        // When
        services.AddCqrs(opt => opt.AddQueryHandler<TestQueryHandler>(lifetime));

        // Then
        services.ShouldContain(
            elementPredicate: descriptor => 
                descriptor.ServiceType == typeof(IQueryHandler<TestQuery, TestQueryResult>)
                && descriptor.ImplementationType == typeof(TestQueryHandler)
                && descriptor.Lifetime == lifetime,
            expectedCount: 1
        );
    }

    [Fact]
    public void WhenCalledMultipleTimes_ShouldRegisterQueryHandlerOnce()
    {
        // Given
        ServiceCollection services = new();

        // When
        services.AddCqrs(opt => opt
            .AddQueryHandler<TestQueryHandler>()
            .AddQueryHandler<TestQueryHandler>()
            .AddQueryHandler<TestQueryHandler>());

        services.AddCqrs(opt => opt
            .AddQueryHandler<TestQueryHandler>());

        // Then
        services.ShouldContain(
            elementPredicate: descriptor => 
                descriptor.ServiceType == typeof(IQueryHandler<TestQuery, TestQueryResult>)
                && descriptor.ImplementationType == typeof(TestQueryHandler)
                && descriptor.Lifetime == ServiceLifetime.Transient,
            expectedCount: 1
        );
    }

    public class SecondTestQuery {}
    public class SecondTestQueryResult {}
    [ExcludeFromCodeCoverage]
    public class SecondTestQueryHandler : IQueryHandler<SecondTestQuery, SecondTestQueryResult>
    {
        public ValueTask<SecondTestQueryResult> Handle(SecondTestQuery Query, CancellationToken cancellation)
        {
            throw new NotImplementedException();
        }
    }

    public class ThirdTestQuery {}
    public class ThirdTestQueryResult {}
    [ExcludeFromCodeCoverage]
    public class ThirdTestQueryHandler : IQueryHandler<ThirdTestQuery, ThirdTestQueryResult>
    {
        public ValueTask<ThirdTestQueryResult> Handle(ThirdTestQuery Query, CancellationToken cancellation)
        {
            throw new NotImplementedException();
        }
    }

    [Fact]
    public void WhenCalledMultipleTimes_ShouldRegisterAllQueryHandlers()
    {
        // Given
        ServiceCollection services = new();

        // When
        services.AddCqrs(opt => opt
            .AddQueryHandler<TestQueryHandler>()
            .AddQueryHandler<SecondTestQueryHandler>());
        services.AddCqrs(opt => opt
            .AddQueryHandler<ThirdTestQueryHandler>());

        // Then
        services.ShouldContain(
            elementPredicate: descriptor => 
                descriptor.ServiceType == typeof(IQueryHandler<TestQuery, TestQueryResult>)
                && descriptor.ImplementationType == typeof(TestQueryHandler)
                && descriptor.Lifetime == ServiceLifetime.Transient,
            expectedCount: 1
        );
        services.ShouldContain(
            elementPredicate: descriptor => 
                descriptor.ServiceType == typeof(IQueryHandler<SecondTestQuery, SecondTestQueryResult>)
                && descriptor.ImplementationType == typeof(SecondTestQueryHandler)
                && descriptor.Lifetime == ServiceLifetime.Transient,
            expectedCount: 1
        );
        services.ShouldContain(
            elementPredicate: descriptor => 
                descriptor.ServiceType == typeof(IQueryHandler<ThirdTestQuery, ThirdTestQueryResult>)
                && descriptor.ImplementationType == typeof(ThirdTestQueryHandler)
                && descriptor.Lifetime == ServiceLifetime.Transient,
            expectedCount: 1
        );
    }
}
