using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;

namespace Cqrs.Tests.QueryDispatcherImplTests;

public class Dispatch
{
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

        var queryHandler = Substitute.For<IQueryHandler<TestQuery, TestQueryResult>>();
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

    public class TestOpenBehaviour<TRequest, TResponse> : IDispatchBehaviour<TRequest, TResponse>
        where TRequest : notnull
    {
        private readonly Action<TRequest> _callback;

        public TestOpenBehaviour(Action<TRequest> callback)
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
    public async Task WhenPipelineConfigured_ShouldCallAllBehavioursInOrder()
    {
        // Given
        var testQuery = new TestQuery();
        var testQueryResult = new TestQueryResult();
        List<string> calls = [];

        var queryHandler = Substitute.For<IQueryHandler<TestQuery, TestQueryResult>>();
        queryHandler.Handle(testQuery, CancellationToken.None)
            .Returns(testQueryResult)
            .AndDoes(_ => calls.Add("queryHandler"));

        var firstBehaviour = Substitute.For<IDispatchBehaviour<TestQuery, TestQueryResult>>();
        firstBehaviour.Handle(testQuery, Arg.Any<DispatchFurtherDelegate<TestQueryResult>>(), Arg.Any<CancellationToken>())
            .Returns(args => ((DispatchFurtherDelegate<TestQueryResult>)args[1]).Invoke())
            .AndDoes(_ => calls.Add("firstBehaviour"));

        var secondBehaviour = Substitute.For<IDispatchBehaviour<TestQuery, TestQueryResult>>();
        secondBehaviour.Handle(testQuery, Arg.Any<DispatchFurtherDelegate<TestQueryResult>>(), Arg.Any<CancellationToken>())
            .Returns(args => ((DispatchFurtherDelegate<TestQueryResult>)args[1]).Invoke())
            .AndDoes(_ => calls.Add("secondBehaviour"));

        ServiceCollection serviceCollection =
        [
            new ServiceDescriptor(
                typeof(IQueryHandler<TestQuery, TestQueryResult>),
                (IServiceProvider _) => queryHandler,
                ServiceLifetime.Transient
            ),
            new ServiceDescriptor(
                typeof(IDispatchBehaviour<TestQuery, TestQueryResult>),
                (IServiceProvider _) => firstBehaviour,
                ServiceLifetime.Transient
            ),
            new ServiceDescriptor(
                typeof(IDispatchBehaviour<TestQuery, TestQueryResult>),
                (IServiceProvider _) => secondBehaviour,
                ServiceLifetime.Transient
            ),
            new ServiceDescriptor(
                typeof(IDispatchBehaviour<,>),
                typeof(TestOpenBehaviour<,>),
                ServiceLifetime.Transient
            ),
        ];
        serviceCollection.AddTransient<Action<TestQuery>>(_ => (TestQuery _) => calls.Add("thirdBehaviour"));
        var services = serviceCollection.BuildServiceProvider();

        var sut = new QueryDispatcherImpl(services, new ConcurrentMethodsCache());

        // When
        var result = await sut.Dispatch(testQuery, CancellationToken.None);

        // Then
        result.ShouldBeSameAs(testQueryResult);
        await firstBehaviour.Received(1).Handle(testQuery, Arg.Any<DispatchFurtherDelegate<TestQueryResult>>(), Arg.Any<CancellationToken>());
        await secondBehaviour.Received(1).Handle(testQuery, Arg.Any<DispatchFurtherDelegate<TestQueryResult>>(), Arg.Any<CancellationToken>());
        await queryHandler.Received(1).Handle(testQuery, CancellationToken.None);

        calls.ShouldBe(["firstBehaviour", "secondBehaviour", "thirdBehaviour", "queryHandler"]);
    }

    [Fact]
    public async Task WhenNextIsNotCalled_ShouldStopExecutingPipeline()
    {
        // Given
        var testQuery = new TestQuery();
        var testQueryResult = new TestQueryResult();
        var testQueryResultAborted = new TestQueryResult();
        List<string> calls = [];

        var queryHandler = Substitute.For<IQueryHandler<TestQuery, TestQueryResult>>();
        queryHandler.Handle(testQuery, CancellationToken.None)
            .Returns(testQueryResult)
            .AndDoes(_ => calls.Add("queryHandler"));

        var firstBehaviour = Substitute.For<IDispatchBehaviour<TestQuery, TestQueryResult>>();
        firstBehaviour.Handle(testQuery, Arg.Any<DispatchFurtherDelegate<TestQueryResult>>(), Arg.Any<CancellationToken>())
            .Returns(args => ((DispatchFurtherDelegate<TestQueryResult>)args[1]).Invoke())
            .AndDoes(_ => calls.Add("firstBehaviour"));

        var secondBehaviour = Substitute.For<IDispatchBehaviour<TestQuery, TestQueryResult>>();
        secondBehaviour.Handle(testQuery, Arg.Any<DispatchFurtherDelegate<TestQueryResult>>(), Arg.Any<CancellationToken>())
            .Returns(args => ValueTask.FromResult(testQueryResultAborted))
            .AndDoes(_ => calls.Add("secondBehaviour"));

        ServiceCollection serviceCollection =
        [
            new ServiceDescriptor(
                typeof(IQueryHandler<TestQuery, TestQueryResult>),
                (IServiceProvider _) => queryHandler,
                ServiceLifetime.Transient
            ),
            new ServiceDescriptor(
                typeof(IDispatchBehaviour<TestQuery, TestQueryResult>),
                (IServiceProvider _) => firstBehaviour,
                ServiceLifetime.Transient
            ),
            new ServiceDescriptor(
                typeof(IDispatchBehaviour<TestQuery, TestQueryResult>),
                (IServiceProvider _) => secondBehaviour,
                ServiceLifetime.Transient
            ),
            new ServiceDescriptor(
                typeof(IDispatchBehaviour<,>),
                typeof(TestOpenBehaviour<,>),
                ServiceLifetime.Transient
            ),
        ];
        serviceCollection.AddTransient<Action<TestQuery>>(_ => (TestQuery _) => calls.Add("thirdBehaviour"));
        var services = serviceCollection.BuildServiceProvider();

        var sut = new QueryDispatcherImpl(services, new ConcurrentMethodsCache());

        // When
        var result = await sut.Dispatch(testQuery, CancellationToken.None);

        // Then
        result.ShouldBeSameAs(testQueryResultAborted);
        await firstBehaviour.Received(1).Handle(testQuery, Arg.Any<DispatchFurtherDelegate<TestQueryResult>>(), Arg.Any<CancellationToken>());
        await secondBehaviour.Received(1).Handle(testQuery, Arg.Any<DispatchFurtherDelegate<TestQueryResult>>(), Arg.Any<CancellationToken>());
        await queryHandler.Received(0).Handle(testQuery, CancellationToken.None);

        calls.ShouldBe(["firstBehaviour", "secondBehaviour"]);
    }
}
