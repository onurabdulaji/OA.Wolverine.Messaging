# OA.Wolverine.Messaging

Lean infrastructure glue that binds OA CQRS abstractions to Wolverine's `IMessageBus` without dictating how applications shape transports, hosts, or policies.

## What Is OA.Wolverine.Messaging
- Dependency injection extension that registers Wolverine-backed implementations of `ICommandBus` and `IQueryBus`
- Thin adapter translating OA CQRS contracts (`ICommand`, `IQuery`, `Result<T>`) into Wolverine invocation semantics
- Persistently minimal: the seam between application-facing buses and Wolverine's runtime primitives, nothing else

## Design Philosophy
### Application-first
Applications own contracts, handlers, failure semantics, transports, and policies. This package only exposes the bus boundary so the rest of the stack can remain host-agnostic.

### Framework-agnostic
No assumptions about ASP.NET Core, Minimal APIs, workers, schedulers, or background hosts. If a container can register services and Wolverine can be configured, the framework fits.

### CQRS Enforced by Contracts
Separate `ICommand<Result<T>>` and `IQuery<Result<T>>` contracts gate access to the buses. Compile-time signatures require explicit `Result` envelopes so CQRS intent is enforced by the type system rather than conventions.

## What This Framework Does
- Registers Wolverine-backed `ICommandBus` and `IQueryBus`
- Dispatches commands/queries via `IMessageBus.InvokeAsync<T>` while preserving `Result` envelopes
- Keeps the infrastructure seam intentionally narrow so transports, middleware, and execution policies remain application-owned

## What This Framework Does NOT Do (by design)
- Does not host Wolverine, configure transports, or manage persistence
- Does not provide middleware, validation, error translation, or retries
- Does not define command/query handlers, serialization rules, or `Result` mapping conventions
- Does not surface Wolverine primitives to application code

## How It Fits Into an Application
### Application layer
Defines contracts, handlers, orchestration logic, and `Result` semantics. Depends only on OA abstractions and consumes `ICommandBus` / `IQueryBus`.

### Infrastructure layer
References this package plus Wolverine. Registers the buses, configures Wolverine transports, persistence choices, and runtime policies.

### Transport layer (HTTP, messaging, background jobs)
Controllers, Minimal APIs, message listeners, or schedulers trigger commands/queries. They interact with the buses, not with Wolverine itself, and keep routing/serialization concerns outside this framework.

## Minimal Usage Example
```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using OA.Abstractions.CQRS;
using OA.Result.Results;
using OA.Wolverine.Messaging;
using Wolverine;

// Command & Query contracts (application layer)
public record CreateProductCommand(string Name, decimal Price)
    : ICommand<Result<Guid>>;

public record GetProductQuery(Guid Id)
    : IQuery<Result<ProductResponse>>;

public record ProductResponse(Guid Id, string Name, decimal Price);

// Handlers (application layer)
public sealed class CreateProductHandler : ICommandHandler<CreateProductCommand, Result<Guid>>
{
    public Task<Result<Guid>> HandleAsync(CreateProductCommand command, CancellationToken ct)
    {
        if (command.Price <= 0m)
            return Task.FromResult(Result<Guid>.Fail("Price must be positive"));

        var id = Guid.NewGuid();
        return Task.FromResult(Result<Guid>.Success(id));
    }
}

public sealed class GetProductHandler : IQueryHandler<GetProductQuery, Result<ProductResponse>>
{
    public Task<Result<ProductResponse>> HandleAsync(GetProductQuery query, CancellationToken ct)
    {
        // Application-owned persistence and mapping
        var product = new ProductResponse(query.Id, "Sample", 42m);
        return Task.FromResult(Result<ProductResponse>.Success(product));
    }
}

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddWolverine();            // Applications own Wolverine configuration
builder.Services.AddOaCqrsBuses();          // Registers ICommandBus and IQueryBus

var app = builder.Build();

// HTTP endpoints (transport layer)
app.MapPost("/products", async (CreateProductRequest request, ICommandBus commandBus) =>
{
    var result = await commandBus.SendAsync(new CreateProductCommand(request.Name, request.Price));
    return result.IsSuccess
        ? Results.Created($"/products/{result.Value}", result.Value)
        : Results.BadRequest(result.Error);
});

app.MapGet("/products/{id}", async (Guid id, IQueryBus queryBus) =>
{
    var result = await queryBus.QueryAsync(new GetProductQuery(id));
    return result.IsSuccess
        ? Results.Ok(result.Value)
        : Results.NotFound(result.Error);
});

app.Run();

public record CreateProductRequest(string Name, decimal Price);
```

## Why Wolverine
- Unified execution runtime for in-process dispatch, durable queues, and remote transports without leaking infrastructure details into the application layer
- Mature diagnostics, handler pipelines, and resiliency primitives configurable exclusively within the infrastructure layer
- Allows infrastructure teams to evolve transports or persistence without touching command/query contracts

## Extensibility & Evolution
- Decorate the registered buses (logging, tracing, resilience) without modifying handlers
- Layer additional Wolverine middleware, transports, or persistence modules independently of this package
- Keep the public API intentionally small (DI extension + bus implementations) so future enhancements can add optional hooks without breaking consumers
