using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;

namespace Cqrs.Tests.QueryDispatcherImplTests;

public class Dispatch
{
    public abstract class TestQueryHandler : IQueryHandler<TestQuery, TestQueryResult>
    {
        public abstract ValueTask<TestQueryResult> Handle(TestQuery query, CancellationToken cancellation);
    }
    public class TestQuery : IKnownQuery<TestQueryResult> {}
    public class TestQueryResult {}

    [Theory]
    [InlineData(ServiceLifetime.Transient)]
    [InlineData(ServiceLifetime.Scoped)]
    [InlineData(ServiceLifetime.Singleton)]
    public async Task WhenCalled_ShouldExecuteHandler(ServiceLifetime lifetime)
    {
        // Given
        var testQuery = new TestQuery();
        var testQueryResult = new TestQueryResult();

        var queryHandler = Substitute.For<TestQueryHandler>();
        queryHandler.Handle(testQuery, CancellationToken.None).Returns(testQueryResult);

        ServiceCollection serviceCollection =
        [
            new ServiceDescriptor(
                typeof(IQueryHandler<TestQuery, TestQueryResult>),
                (IServiceProvider _) => queryHandler,
                lifetime
            ),
        ];
        var services = serviceCollection.BuildServiceProvider();

        var sut = new QueryDispatcherImpl(services, new ConcurrentMethodsCache());

        // When
        var result = await sut.Dispatch(testQuery, CancellationToken.None);

        // Then
        result.ShouldBeSameAs(testQueryResult);
        await queryHandler.Received(1).Handle(testQuery, CancellationToken.None);
    }

    public class TestOpenBehavior<TRequest, TResponse> : IDispatchBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        private readonly Action<TRequest> _callback;

        public TestOpenBehavior(Action<TRequest> callback)
        {
            _callback = callback;
        }

        public ValueTask<TResponse> Handle(TRequest request, DispatchFurtherDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            _callback.Invoke(request);
            return next();
        }
    }

    [Fact]
    public async Task WhenPipelineConfigured_ShouldCallAllBehaviorsInOrder()
    {
        // Given
        var testQuery = new TestQuery();
        var testQueryResult = new TestQueryResult();
        List<string> calls = [];

        var queryHandler = Substitute.For<TestQueryHandler>();
        queryHandler.Handle(testQuery, CancellationToken.None)
            .Returns(testQueryResult)
            .AndDoes(_ => calls.Add("queryHandler"));

        var firstBehavior = Substitute.For<IDispatchBehavior<TestQuery, TestQueryResult>>();
        firstBehavior.Handle(testQuery, Arg.Any<DispatchFurtherDelegate<TestQueryResult>>(), Arg.Any<CancellationToken>())
            .Returns(args => ((DispatchFurtherDelegate<TestQueryResult>)args[1]).Invoke())
            .AndDoes(_ => calls.Add("firstBehavior"));

        var secondBehavior = Substitute.For<IDispatchBehavior<TestQuery, TestQueryResult>>();
        secondBehavior.Handle(testQuery, Arg.Any<DispatchFurtherDelegate<TestQueryResult>>(), Arg.Any<CancellationToken>())
            .Returns(args => ((DispatchFurtherDelegate<TestQueryResult>)args[1]).Invoke())
            .AndDoes(_ => calls.Add("secondBehavior"));

        ServiceCollection serviceCollection =
        [
            new ServiceDescriptor(
                typeof(IQueryHandler<TestQuery, TestQueryResult>),
                (IServiceProvider _) => queryHandler,
                ServiceLifetime.Transient
            ),
            new ServiceDescriptor(
                typeof(IDispatchBehavior<TestQuery, TestQueryResult>),
                (IServiceProvider _) => firstBehavior,
                ServiceLifetime.Transient
            ),
            new ServiceDescriptor(
                typeof(IDispatchBehavior<TestQuery, TestQueryResult>),
                (IServiceProvider _) => secondBehavior,
                ServiceLifetime.Transient
            ),
            new ServiceDescriptor(
                typeof(IDispatchBehavior<,>),
                typeof(TestOpenBehavior<,>),
                ServiceLifetime.Transient
            ),
        ];
        serviceCollection.AddTransient<Action<TestQuery>>(_ => (TestQuery _) => calls.Add("thirdBehavior"));
        var services = serviceCollection.BuildServiceProvider();

        var sut = new QueryDispatcherImpl(services, new ConcurrentMethodsCache());

        // When
        var result = await sut.Dispatch(testQuery, CancellationToken.None);

        // Then
        result.ShouldBeSameAs(testQueryResult);
        await firstBehavior.Received(1).Handle(testQuery, Arg.Any<DispatchFurtherDelegate<TestQueryResult>>(), Arg.Any<CancellationToken>());
        await secondBehavior.Received(1).Handle(testQuery, Arg.Any<DispatchFurtherDelegate<TestQueryResult>>(), Arg.Any<CancellationToken>());
        await queryHandler.Received(1).Handle(testQuery, CancellationToken.None);

        calls.ShouldBe(["firstBehavior", "secondBehavior", "thirdBehavior", "queryHandler"]);
    }

    [Fact]
    public async Task WhenNextIsNotCalled_ShouldStopExecutingPipeline()
    {
        // Given
        var testQuery = new TestQuery();
        var testQueryResult = new TestQueryResult();
        var testQueryResultAborted = new TestQueryResult();
        List<string> calls = [];

        var queryHandler = Substitute.For<TestQueryHandler>();
        queryHandler.Handle(testQuery, CancellationToken.None)
            .Returns(testQueryResult)
            .AndDoes(_ => calls.Add("queryHandler"));

        var firstBehavior = Substitute.For<IDispatchBehavior<TestQuery, TestQueryResult>>();
        firstBehavior.Handle(testQuery, Arg.Any<DispatchFurtherDelegate<TestQueryResult>>(), Arg.Any<CancellationToken>())
            .Returns(args => ((DispatchFurtherDelegate<TestQueryResult>)args[1]).Invoke())
            .AndDoes(_ => calls.Add("firstBehavior"));

        var secondBehavior = Substitute.For<IDispatchBehavior<TestQuery, TestQueryResult>>();
        secondBehavior.Handle(testQuery, Arg.Any<DispatchFurtherDelegate<TestQueryResult>>(), Arg.Any<CancellationToken>())
            .Returns(args => ValueTask.FromResult(testQueryResultAborted))
            .AndDoes(_ => calls.Add("secondBehavior"));

        ServiceCollection serviceCollection =
        [
            new ServiceDescriptor(
                typeof(IQueryHandler<TestQuery, TestQueryResult>),
                (IServiceProvider _) => queryHandler,
                ServiceLifetime.Transient
            ),
            new ServiceDescriptor(
                typeof(IDispatchBehavior<TestQuery, TestQueryResult>),
                (IServiceProvider _) => firstBehavior,
                ServiceLifetime.Transient
            ),
            new ServiceDescriptor(
                typeof(IDispatchBehavior<TestQuery, TestQueryResult>),
                (IServiceProvider _) => secondBehavior,
                ServiceLifetime.Transient
            ),
            new ServiceDescriptor(
                typeof(IDispatchBehavior<,>),
                typeof(TestOpenBehavior<,>),
                ServiceLifetime.Transient
            ),
        ];
        serviceCollection.AddTransient<Action<TestQuery>>(_ => (TestQuery _) => calls.Add("thirdBehavior"));
        var services = serviceCollection.BuildServiceProvider();

        var sut = new QueryDispatcherImpl(services, new ConcurrentMethodsCache());

        // When
        var result = await sut.Dispatch(testQuery, CancellationToken.None);

        // Then
        result.ShouldBeSameAs(testQueryResultAborted);
        await firstBehavior.Received(1).Handle(testQuery, Arg.Any<DispatchFurtherDelegate<TestQueryResult>>(), Arg.Any<CancellationToken>());
        await secondBehavior.Received(1).Handle(testQuery, Arg.Any<DispatchFurtherDelegate<TestQueryResult>>(), Arg.Any<CancellationToken>());
        await queryHandler.Received(0).Handle(testQuery, CancellationToken.None);

        calls.ShouldBe(["firstBehavior", "secondBehavior"]);
    }

    public abstract class AnotherTestQueryHandler : IQueryHandler<TestQuery, AnotherTestQueryResult>
    {
        public abstract ValueTask<AnotherTestQueryResult> Handle(TestQuery query, CancellationToken cancellation);
    }
    public class AnotherTestQueryResult {}

    [Fact]
    public async Task WhenTwoHandlersWithDifferentResultTypesRegisteredForOneQueryType_ShouldCorrectlyDispatchToBoth() // Fix to Cache Key Collision
    {
        // Given
        var testQuery = new TestQuery();
        var testQueryResult = new TestQueryResult();
        var anotherTestQueryResult = new AnotherTestQueryResult();
    
        var queryHandler = Substitute.For<TestQueryHandler>();
        queryHandler.Handle(testQuery, CancellationToken.None).Returns(testQueryResult);

        var anotherQueryHandler = Substitute.For<AnotherTestQueryHandler>();
        anotherQueryHandler.Handle(testQuery, CancellationToken.None).Returns(anotherTestQueryResult);

        ServiceCollection serviceCollection =
        [
            new ServiceDescriptor(
                typeof(IQueryHandler<TestQuery, TestQueryResult>),
                (IServiceProvider _) => queryHandler,
                ServiceLifetime.Transient
            ),
            new ServiceDescriptor(
                typeof(IQueryHandler<TestQuery, AnotherTestQueryResult>),
                (IServiceProvider _) => anotherQueryHandler,
                ServiceLifetime.Transient
            ),
        ];
        var services = serviceCollection.BuildServiceProvider();

        var sut = new QueryDispatcherImpl(services, new ConcurrentMethodsCache());

        // When
        var result = await sut.Dispatch(testQuery, CancellationToken.None);
        var anotherResult = await sut.Dispatch<AnotherTestQueryResult>(testQuery, CancellationToken.None);

        // Then
        result.ShouldBeSameAs(testQueryResult);
        await queryHandler.Received(1).Handle(testQuery, CancellationToken.None);

        anotherResult.ShouldBeSameAs(anotherTestQueryResult);
        await anotherQueryHandler.Received(1).Handle(testQuery, CancellationToken.None);
    }
}
