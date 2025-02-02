# Just.Cqrs

Inspired by [MediatR](https://github.com/jbogard/MediatR)

**Just.Cqrs** is a lightweight, easy-to-use C# library designed to simplify the implementation of the Command Query Responsibility Segregation (CQRS) pattern in .NET applications. With a focus on simplicity and flexibility, **Just.Cqrs** provides a clean and intuitive way to separate command and query logic.

## Features

* Separate dispatching of Commands/Queries
* Middleware-like Behaviors

## Compatibility

**Just.Cqrs** is built for .Net Standard 2.1 and .NET 8.0 and 9.0.

## Getting Started

### Install from NuGet.org

```bash
# install the package using NuGet
dotnet add package Just.Cqrs
```

### Register in DI with ```IServiceCollection```

```csharp
services.AddCqrs(opt => opt
    .AddQueryHandler<SomeQueryHandler>()
    .AddCommandHandler<SomeCommandHandler>()
    .AddBehavior<SomeQueryBehavior>()
    .AddOpenBehavior(typeof(LoggingBehavior<,>))
);
```

## Example Usage

### Define a Query and Handler

```csharp
record GetUserByIdQuery(int UserId) : IKnownQuery<User>;

class GetUserByIdQueryHandler : IQueryHandler<GetUserByIdQuery, User>
{
    public ValueTask<User> Handle(GetUserByIdQuery query, CancellationToken cancellationToken)
    {
        // Fetch user logic here
    }
}

// Use Dispatcher to execute the query
class GetUserByIdUseCase(IQueryDispatcher dispatcher)
{
    public async Task<IResult> Execute(int userId, CancellationToken cancellationToken)
    {
        var user = await dispatcher.Dispatch(new GetUserByIdQuery(userId), cancellationToken);
    }
}
```

\* *the same principles apply to commands*

### Define a Behavior

```csharp
class LoggingBehavior<TRequest, TResult>(ILogger logger) : IDispatchBehavior<TRequest, TResult>
    where TRequest: notnull
{
    public async ValueTask<TResult> Handle(
        TRequest request,
        DispatchFurtherDelegate<TResult> next,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Handling request: {RequestType}", typeof(TRequest).Name);
        var result = await next();
        logger.LogInformation("Request handled: {RequestType}", typeof(TRequest).Name);
        return result;
    }
}

class SomeQueryBehavior : IDispatchBehavior<SomeQuery, SomeQueryResult>
{
    public async ValueTask<SomeQueryResult> Handle(
        SomeQuery request,
        DispatchFurtherDelegate<SomeQueryResult> next,
        CancellationToken cancellationToken)
    {
        // do something
        return await next();
    }
}
```

## License

**Just.Cqrs** is licensed under the [MIT License](LICENSE).
