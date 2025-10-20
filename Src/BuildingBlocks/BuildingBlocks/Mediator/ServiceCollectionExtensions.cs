using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Mediator;

// <summary>
/// Extensões para configuração do Mediator no container de DI
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registra o Mediator e todos os handlers encontrados nos assemblies especificados
    /// </summary>
    /// <param name="services">Collection de serviços</param>
    /// <param name="assemblies">Assemblies onde buscar os handlers</param>
    /// <returns>ServiceCollection para fluent interface</returns>
    public static IServiceCollection AddMediator(this IServiceCollection services, params Assembly[] assemblies)
    {
        // Registra o Mediator como Singleton
        services.AddSingleton<IMediator, Mediator>();

        // Se nenhum assembly foi especificado, usa o assembly que está chamando
        if (assemblies.Length == 0)
        {
            assemblies = new[] { Assembly.GetCallingAssembly() };
        }

        // Registra todos os handlers encontrados
        RegisterHandlers(services, assemblies);

        return services;
    }

    /// <summary>
    /// Registra o Mediator e busca handlers no assembly especificado
    /// </summary>
    /// <param name="services">Collection de serviços</param>
    /// <param name="assembly">Assembly onde buscar os handlers</param>
    /// <returns>ServiceCollection para fluent interface</returns>
    public static IServiceCollection AddMediator(this IServiceCollection services, Assembly assembly)
    {
        return services.AddMediator(new[] { assembly });
    }

    /// <summary>
    /// Registra o Mediator e busca handlers automaticamente no assembly que está chamando
    /// </summary>
    /// <param name="services">Collection de serviços</param>
    /// <returns>ServiceCollection para fluent interface</returns>
    public static IServiceCollection AddMediator(this IServiceCollection services)
    {
        return services.AddMediator(Assembly.GetCallingAssembly());
    }

    private static void RegisterHandlers(IServiceCollection services, Assembly[] assemblies)
    {
        foreach (var assembly in assemblies)
        {
            // Busca todos os tipos que implementam IRequestHandler
            var requestHandlerTypes = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract)
                .Where(t => t.GetInterfaces().Any(i => 
                    i.IsGenericType && 
                    (i.GetGenericTypeDefinition() == typeof(IRequestHandler<>) ||
                     i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>))))
                .ToList();

            foreach (var handlerType in requestHandlerTypes)
            {
                var interfaces = handlerType.GetInterfaces()
                    .Where(i => i.IsGenericType && 
                               (i.GetGenericTypeDefinition() == typeof(IRequestHandler<>) ||
                                i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>)));

                foreach (var interfaceType in interfaces)
                {
                    services.AddTransient(interfaceType, handlerType);
                }
            }

            // Busca todos os tipos que implementam INotificationHandler
            var notificationHandlerTypes = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract)
                .Where(t => t.GetInterfaces().Any(i => 
                    i.IsGenericType && 
                    i.GetGenericTypeDefinition() == typeof(INotificationHandler<>)))
                .ToList();

            foreach (var handlerType in notificationHandlerTypes)
            {
                var interfaces = handlerType.GetInterfaces()
                    .Where(i => i.IsGenericType && 
                               i.GetGenericTypeDefinition() == typeof(INotificationHandler<>));

                foreach (var interfaceType in interfaces)
                {
                    services.AddTransient(interfaceType, handlerType);
                }
            }
        }
    }
}