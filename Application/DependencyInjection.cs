using Application.Common.Behaviors;
using Application.Common.Interfaces;
using Domain.Common;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registers all Application layer services: Command/Query handlers, validators, and validation decorators.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        RegisterHandlers(services, assembly);

        services.AddValidatorsFromAssembly(assembly);

        return services;
    }

    private static void RegisterHandlers(IServiceCollection services, Assembly assembly)
    {
        var handlerTypes = assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .SelectMany(t => t.GetInterfaces(), (implType, ifaceType) => new { Implementation = implType, Interface = ifaceType })
            .Where(x => x.Interface.IsGenericType && x.Interface.GetGenericTypeDefinition() == typeof(IHandler<,>))
            .ToList();

        foreach (var handler in handlerTypes)
        {
            // Register concrete class directly in DI (so the decorator or callers can resolve it)
            services.AddScoped(handler.Implementation);

            // Register the interface with decorator logic
            services.AddScoped(handler.Interface, provider =>
            {
                var impl = provider.GetRequiredService(handler.Implementation);
                var requestType = handler.Interface.GetGenericArguments()[0];
                var responseType = handler.Interface.GetGenericArguments()[1];

                if (typeof(Result).IsAssignableFrom(responseType))
                {
                    var decoratorType = typeof(ValidationHandlerDecorator<,>).MakeGenericType(requestType, responseType);
                    return Activator.CreateInstance(decoratorType, impl, provider)!;
                }

                return impl;
            });
        }
    }
}
