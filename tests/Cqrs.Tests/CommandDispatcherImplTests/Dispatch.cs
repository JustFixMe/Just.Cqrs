using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;

namespace Cqrs.Tests.CommandDispatcherImplTests;

public class Dispatch
{
    public abstract class TestCommandHandler : ICommandHandler<TestCommand, TestCommandResult>
    {
        public abstract ValueTask<TestCommandResult> Handle(TestCommand command, CancellationToken cancellation);
    }
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

        var commandHandler = Substitute.For<TestCommandHandler>();
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
        var testCommand = new TestCommand();
        var testCommandResult = new TestCommandResult();
        List<string> calls = [];

        var commandHandler = Substitute.For<TestCommandHandler>();
        commandHandler.Handle(testCommand, CancellationToken.None)
            .Returns(testCommandResult)
            .AndDoes(_ => calls.Add("commandHandler"));

        var firstBehavior = Substitute.For<IDispatchBehavior<TestCommand, TestCommandResult>>();
        firstBehavior.Handle(testCommand, Arg.Any<DispatchFurtherDelegate<TestCommandResult>>(), Arg.Any<CancellationToken>())
            .Returns(args => ((DispatchFurtherDelegate<TestCommandResult>)args[1]).Invoke())
            .AndDoes(_ => calls.Add("firstBehavior"));

        var secondBehavior = Substitute.For<IDispatchBehavior<TestCommand, TestCommandResult>>();
        secondBehavior.Handle(testCommand, Arg.Any<DispatchFurtherDelegate<TestCommandResult>>(), Arg.Any<CancellationToken>())
            .Returns(args => ((DispatchFurtherDelegate<TestCommandResult>)args[1]).Invoke())
            .AndDoes(_ => calls.Add("secondBehavior"));

        ServiceCollection serviceCollection =
        [
            new ServiceDescriptor(
                typeof(ICommandHandler<TestCommand, TestCommandResult>),
                (IServiceProvider _) => commandHandler,
                ServiceLifetime.Transient
            ),
            new ServiceDescriptor(
                typeof(IDispatchBehavior<TestCommand, TestCommandResult>),
                (IServiceProvider _) => firstBehavior,
                ServiceLifetime.Transient
            ),
            new ServiceDescriptor(
                typeof(IDispatchBehavior<TestCommand, TestCommandResult>),
                (IServiceProvider _) => secondBehavior,
                ServiceLifetime.Transient
            ),
            new ServiceDescriptor(
                typeof(IDispatchBehavior<,>),
                typeof(TestOpenBehavior<,>),
                ServiceLifetime.Transient
            ),
        ];
        serviceCollection.AddTransient<Action<TestCommand>>(_ => (TestCommand _) => calls.Add("thirdBehavior"));
        var services = serviceCollection.BuildServiceProvider();

        var sut = new CommandDispatcherImpl(services, new ConcurrentMethodsCache());

        // When
        var result = await sut.Dispatch(testCommand, CancellationToken.None);

        // Then
        result.ShouldBeSameAs(testCommandResult);
        await firstBehavior.Received(1).Handle(testCommand, Arg.Any<DispatchFurtherDelegate<TestCommandResult>>(), Arg.Any<CancellationToken>());
        await secondBehavior.Received(1).Handle(testCommand, Arg.Any<DispatchFurtherDelegate<TestCommandResult>>(), Arg.Any<CancellationToken>());
        await commandHandler.Received(1).Handle(testCommand, CancellationToken.None);

        calls.ShouldBe(["firstBehavior", "secondBehavior", "thirdBehavior", "commandHandler"]);
    }

    [Fact]
    public async Task WhenNextIsNotCalled_ShouldStopExecutingPipeline()
    {
        // Given
        var testCommand = new TestCommand();
        var testCommandResult = new TestCommandResult();
        var testCommandResultAborted = new TestCommandResult();
        List<string> calls = [];

        var commandHandler = Substitute.For<TestCommandHandler>();
        commandHandler.Handle(testCommand, CancellationToken.None)
            .Returns(testCommandResult)
            .AndDoes(_ => calls.Add("commandHandler"));

        var firstBehavior = Substitute.For<IDispatchBehavior<TestCommand, TestCommandResult>>();
        firstBehavior.Handle(testCommand, Arg.Any<DispatchFurtherDelegate<TestCommandResult>>(), Arg.Any<CancellationToken>())
            .Returns(args => ((DispatchFurtherDelegate<TestCommandResult>)args[1]).Invoke())
            .AndDoes(_ => calls.Add("firstBehavior"));

        var secondBehavior = Substitute.For<IDispatchBehavior<TestCommand, TestCommandResult>>();
        secondBehavior.Handle(testCommand, Arg.Any<DispatchFurtherDelegate<TestCommandResult>>(), Arg.Any<CancellationToken>())
            .Returns(args => ValueTask.FromResult(testCommandResultAborted))
            .AndDoes(_ => calls.Add("secondBehavior"));

        ServiceCollection serviceCollection =
        [
            new ServiceDescriptor(
                typeof(ICommandHandler<TestCommand, TestCommandResult>),
                (IServiceProvider _) => commandHandler,
                ServiceLifetime.Transient
            ),
            new ServiceDescriptor(
                typeof(IDispatchBehavior<TestCommand, TestCommandResult>),
                (IServiceProvider _) => firstBehavior,
                ServiceLifetime.Transient
            ),
            new ServiceDescriptor(
                typeof(IDispatchBehavior<TestCommand, TestCommandResult>),
                (IServiceProvider _) => secondBehavior,
                ServiceLifetime.Transient
            ),
            new ServiceDescriptor(
                typeof(IDispatchBehavior<,>),
                typeof(TestOpenBehavior<,>),
                ServiceLifetime.Transient
            ),
        ];
        serviceCollection.AddTransient<Action<TestCommand>>(_ => (TestCommand _) => calls.Add("thirdBehavior"));
        var services = serviceCollection.BuildServiceProvider();

        var sut = new CommandDispatcherImpl(services, new ConcurrentMethodsCache());

        // When
        var result = await sut.Dispatch(testCommand, CancellationToken.None);

        // Then
        result.ShouldBeSameAs(testCommandResultAborted);
        await firstBehavior.Received(1).Handle(testCommand, Arg.Any<DispatchFurtherDelegate<TestCommandResult>>(), Arg.Any<CancellationToken>());
        await secondBehavior.Received(1).Handle(testCommand, Arg.Any<DispatchFurtherDelegate<TestCommandResult>>(), Arg.Any<CancellationToken>());
        await commandHandler.Received(0).Handle(testCommand, CancellationToken.None);

        calls.ShouldBe(["firstBehavior", "secondBehavior"]);
    }

    public abstract class AnotherTestCommandHandler : ICommandHandler<TestCommand, AnotherTestCommandResult>
    {
        public abstract ValueTask<AnotherTestCommandResult> Handle(TestCommand command, CancellationToken cancellation);
    }
    public class AnotherTestCommandResult {}

    [Fact]
    public async Task WhenTwoHandlersWithDifferentResultTypesRegisteredForOneCommandType_ShouldCorrectlyDispatchToBoth() // Fix to Cache Key Collision
    {
        // Given
        var testCommand = new TestCommand();
        var testCommandResult = new TestCommandResult();
        var anotherTestCommandResult = new AnotherTestCommandResult();
    
        var commandHandler = Substitute.For<TestCommandHandler>();
        commandHandler.Handle(testCommand, CancellationToken.None).Returns(testCommandResult);

        var anotherCommandHandler = Substitute.For<AnotherTestCommandHandler>();
        anotherCommandHandler.Handle(testCommand, CancellationToken.None).Returns(anotherTestCommandResult);

        ServiceCollection serviceCollection =
        [
            new ServiceDescriptor(
                typeof(ICommandHandler<TestCommand, TestCommandResult>),
                (IServiceProvider _) => commandHandler,
                ServiceLifetime.Transient
            ),
            new ServiceDescriptor(
                typeof(ICommandHandler<TestCommand, AnotherTestCommandResult>),
                (IServiceProvider _) => anotherCommandHandler,
                ServiceLifetime.Transient
            ),
        ];
        var services = serviceCollection.BuildServiceProvider();

        var sut = new CommandDispatcherImpl(services, new ConcurrentMethodsCache());

        // When
        var result = await sut.Dispatch(testCommand, CancellationToken.None);
        var anotherResult = await sut.Dispatch<AnotherTestCommandResult>(testCommand, CancellationToken.None);

        // Then
        result.ShouldBeSameAs(testCommandResult);
        await commandHandler.Received(1).Handle(testCommand, CancellationToken.None);

        anotherResult.ShouldBeSameAs(anotherTestCommandResult);
        await anotherCommandHandler.Received(1).Handle(testCommand, CancellationToken.None);
    }
}
