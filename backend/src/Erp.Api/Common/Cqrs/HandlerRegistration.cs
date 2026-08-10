using System.Reflection;
using Erp.Api.Common.Cqrs;
using Microsoft.Extensions.DependencyInjection;

namespace Erp.Api.Common.Cqrs;

/// <summary>
/// Registers every command and query handler in an assembly by convention.
/// <para>
/// The legacy <c>Program.cs</c> held about 105 hand-written <c>AddScoped</c> lines
/// that nobody dared prune, because a registration and its usage were nowhere near
/// each other. Scanning means writing a handler is sufficient to make it available,
/// and deleting one removes its registration automatically.
/// </para>
/// </summary>
public static class HandlerRegistration
{
    private static readonly Type[] HandlerInterfaces =
    [
        typeof(ICommandHandler<,>),
        typeof(IQueryHandler<,>),
    ];

    public static IServiceCollection AddHandlersFromAssembly(this IServiceCollection services, Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assembly);

        var implementations = assembly
            .GetTypes()
            .Where(type => type is { IsAbstract: false, IsInterface: false, IsGenericTypeDefinition: false });

        foreach (var implementation in implementations)
        {
            var contracts = implementation
                .GetInterfaces()
                .Where(contract => contract.IsGenericType
                    && HandlerInterfaces.Contains(contract.GetGenericTypeDefinition()));

            foreach (var contract in contracts)
            {
                services.AddScoped(contract, implementation);
            }
        }

        return services;
    }
}
