using OA.Abstractions.CQRS;
using OA.Result.Results;
using Wolverine;
using ICommandBus = OA.Abstractions.CQRS.ICommandBus;

namespace OA.Wolverine.Messaging;

using Result = Result.Results.Result;

public sealed class WolverineCommandBus : ICommandBus
{
    private readonly IMessageBus _bus;

    public WolverineCommandBus(IMessageBus bus)
    {
        _bus = bus;
    }

    public Task<Result> SendAsync(ICommand command, CancellationToken ct = default)
    {
        return _bus.InvokeAsync<Result>(command, ct);
    }

    public Task<Result<T>> SendAsync<T>(ICommand<Result<T>> command, CancellationToken ct = default)
    {
        return _bus.InvokeAsync<Result<T>>(command, ct);
    }
}

public sealed class WolverineQueryBus : IQueryBus
{
    private readonly IMessageBus _bus;

    public WolverineQueryBus(IMessageBus bus)
    {
        _bus = bus;
    }

    public Task<Result<T>> QueryAsync<T>(IQuery<Result<T>> query, CancellationToken ct = default)
    {
        return _bus.InvokeAsync<Result<T>>(query, ct);
    }
}