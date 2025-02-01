# Just.Cqrs

Inspired by [MediatR](https://github.com/jbogard/MediatR)

**Just.Cqrs** is a lightweight, easy-to-use C# library designed to simplify the implementation of the Command Query Responsibility Segregation (CQRS) pattern in .NET applications. With a focus on simplicity and flexibility, **Just.Cqrs** provides a clean and intuitive way to separate command and query logic.

## Features

* Separate dispatching of Commands/Queries
* Middleware-like behaviours

## Getting Started

### Install from NuGet.org

```
# install the package using NuGet
dotnet add package Just.Cqrs
```

### Register in DI with ```IServiceCollection```

```cs
services.AddCqrs(opt => opt
    .AddQueryHandler<SomeQueryHandler>()
    .AddCommandHandler<SomeCommandHandler>()
    .AddBehaviour<SomeBehaviour>()
    .AddOpenBehaviour(typeof(SomeOpenBehaviour<,>))
);
```
