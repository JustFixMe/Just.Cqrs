using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;

namespace Cqrs.Tests.CommandDispatcherImplTests;

public class Dispatch
{
    public class TestCommand : IKnownCommand<TestCommandResult> {}
    public class TestCommandResult {}

    [Theory]
    [InlineData(ServiceLifetime.Transient)]
    [InlineData(ServiceLifetime.Scoped)]
    [InlineData(ServiceLifetime.Singleton)]
    public async Task WhenCalled_ShouldExecuteHandler(ServiceLifetime lifetime)
    {
        // Given
        var testCommand = new TestCommand();
        var testCommandResult = new TestCommandResult();

        var commandHandler = Substitute.For<ICommandHandler<TestCommand, TestCommandResult>>();
        commandHandler.Handle(testCommand, CancellationToken.None).Returns(testCommandResult);

        ServiceCollection serviceCollection =
        [
            new ServiceDescriptor(
                typeof(ICommandHandler<TestCommand, TestCommandResult>),
                (IServiceProvider _) => commandHandler,
                lifetime
            ),
        ];
        var services = serviceCollection.BuildServiceProvider();

        var sut = new CommandDispatcherImpl(services, new ConcurrentMethodsCache());

        // When
        var result = await sut.Dispatch(testCommand, CancellationToken.None);

        // Then
        result.ShouldBeSameAs(testCommandResult);
        await commandHandler.Received(1).Handle(testCommand, CancellationToken.None);
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
        var testCommand = new TestCommand();
        var testCommandResult = new TestCommandResult();
        List<string> calls = [];

        var commandHandler = Substitute.For<ICommandHandler<TestCommand, TestCommandResult>>();
        commandHandler.Handle(testCommand, CancellationToken.None)
            .Returns(testCommandResult)
            .AndDoes(_ => calls.Add("commandHandler"));

        var firstBehaviour = Substitute.For<IDispatchBehaviour<TestCommand, TestCommandResult>>();
        firstBehaviour.Handle(testCommand, Arg.Any<DispatchFurtherDelegate<TestCommandResult>>(), Arg.Any<CancellationToken>())
            .Returns(args => ((DispatchFurtherDelegate<TestCommandResult>)args[1]).Invoke())
            .AndDoes(_ => calls.Add("firstBehaviour"));

        var secondBehaviour = Substitute.For<IDispatchBehaviour<TestCommand, TestCommandResult>>();
        secondBehaviour.Handle(testCommand, Arg.Any<DispatchFurtherDelegate<TestCommandResult>>(), Arg.Any<CancellationToken>())
            .Returns(args => ((DispatchFurtherDelegate<TestCommandResult>)args[1]).Invoke())
            .AndDoes(_ => calls.Add("secondBehaviour"));

        ServiceCollection serviceCollection =
        [
            new ServiceDescriptor(
                typeof(ICommandHandler<TestCommand, TestCommandResult>),
                (IServiceProvider _) => commandHandler,
                ServiceLifetime.Transient
            ),
            new ServiceDescriptor(
                typeof(IDispatchBehaviour<TestCommand, TestCommandResult>),
                (IServiceProvider _) => firstBehaviour,
                ServiceLifetime.Transient
            ),
            new ServiceDescriptor(
                typeof(IDispatchBehaviour<TestCommand, TestCommandResult>),
                (IServiceProvider _) => secondBehaviour,
                ServiceLifetime.Transient
            ),
            new ServiceDescriptor(
                typeof(IDispatchBehaviour<,>),
                typeof(TestOpenBehaviour<,>),
                ServiceLifetime.Transient
            ),
        ];
        serviceCollection.AddTransient<Action<TestCommand>>(_ => (TestCommand _) => calls.Add("thirdBehaviour"));
        var services = serviceCollection.BuildServiceProvider();

        var sut = new CommandDispatcherImpl(services, new ConcurrentMethodsCache());

        // When
        var result = await sut.Dispatch(testCommand, CancellationToken.None);

        // Then
        result.ShouldBeSameAs(testCommandResult);
        await firstBehaviour.Received(1).Handle(testCommand, Arg.Any<DispatchFurtherDelegate<TestCommandResult>>(), Arg.Any<CancellationToken>());
        await secondBehaviour.Received(1).Handle(testCommand, Arg.Any<DispatchFurtherDelegate<TestCommandResult>>(), Arg.Any<CancellationToken>());
        await commandHandler.Received(1).Handle(testCommand, CancellationToken.None);

        calls.ShouldBe(["firstBehaviour", "secondBehaviour", "thirdBehaviour", "commandHandler"]);
    }

    [Fact]
    public async Task WhenNextIsNotCalled_ShouldStopExecutingPipeline()
    {
        // Given
        var testCommand = new TestCommand();
        var testCommandResult = new TestCommandResult();
        var testCommandResultAborted = new TestCommandResult();
        List<string> calls = [];

        var commandHandler = Substitute.For<ICommandHandler<TestCommand, TestCommandResult>>();
        commandHandler.Handle(testCommand, CancellationToken.None)
            .Returns(testCommandResult)
            .AndDoes(_ => calls.Add("commandHandler"));

        var firstBehaviour = Substitute.For<IDispatchBehaviour<TestCommand, TestCommandResult>>();
        firstBehaviour.Handle(testCommand, Arg.Any<DispatchFurtherDelegate<TestCommandResult>>(), Arg.Any<CancellationToken>())
            .Returns(args => ((DispatchFurtherDelegate<TestCommandResult>)args[1]).Invoke())
            .AndDoes(_ => calls.Add("firstBehaviour"));

        var secondBehaviour = Substitute.For<IDispatchBehaviour<TestCommand, TestCommandResult>>();
        secondBehaviour.Handle(testCommand, Arg.Any<DispatchFurtherDelegate<TestCommandResult>>(), Arg.Any<CancellationToken>())
            .Returns(args => ValueTask.FromResult(testCommandResultAborted))
            .AndDoes(_ => calls.Add("secondBehaviour"));

        ServiceCollection serviceCollection =
        [
            new ServiceDescriptor(
                typeof(ICommandHandler<TestCommand, TestCommandResult>),
                (IServiceProvider _) => commandHandler,
                ServiceLifetime.Transient
            ),
            new ServiceDescriptor(
                typeof(IDispatchBehaviour<TestCommand, TestCommandResult>),
                (IServiceProvider _) => firstBehaviour,
                ServiceLifetime.Transient
            ),
            new ServiceDescriptor(
                typeof(IDispatchBehaviour<TestCommand, TestCommandResult>),
                (IServiceProvider _) => secondBehaviour,
                ServiceLifetime.Transient
            ),
            new ServiceDescriptor(
                typeof(IDispatchBehaviour<,>),
                typeof(TestOpenBehaviour<,>),
                ServiceLifetime.Transient
            ),
        ];
        serviceCollection.AddTransient<Action<TestCommand>>(_ => (TestCommand _) => calls.Add("thirdBehaviour"));
        var services = serviceCollection.BuildServiceProvider();

        var sut = new CommandDispatcherImpl(services, new ConcurrentMethodsCache());

        // When
        var result = await sut.Dispatch(testCommand, CancellationToken.None);

        // Then
        result.ShouldBeSameAs(testCommandResultAborted);
        await firstBehaviour.Received(1).Handle(testCommand, Arg.Any<DispatchFurtherDelegate<TestCommandResult>>(), Arg.Any<CancellationToken>());
        await secondBehaviour.Received(1).Handle(testCommand, Arg.Any<DispatchFurtherDelegate<TestCommandResult>>(), Arg.Any<CancellationToken>());
        await commandHandler.Received(0).Handle(testCommand, CancellationToken.None);

        calls.ShouldBe(["firstBehaviour", "secondBehaviour"]);
    }
}
