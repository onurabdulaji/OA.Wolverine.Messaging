using Microsoft.Extensions.DependencyInjection;
using OA.Abstractions.CQRS;
using ICommandBus = OA.Abstractions.CQRS.ICommandBus;

namespace OA.Wolverine.Messaging;

public static class OaMessagingExtensions
{
    public static IServiceCollection AddOaCqrsBuses(this IServiceCollection services)
    {
        services.AddScoped<ICommandBus, WolverineCommandBus>();
        services.AddScoped<IQueryBus, WolverineQueryBus>();
        return services;
    }
}