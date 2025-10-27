using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace BuildingBlocks.MessageBus;

/// <summary>
/// Extension methods for configuring Message Bus services in the DI container
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Message Bus with default configuration from IConfiguration
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration instance</param>
    /// <param name="assemblies">Assemblies to scan for message handlers</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddMessageBus(
        this IServiceCollection services,
        IConfiguration configuration,
        params Assembly[] assemblies)
    {
        var config = new MessageBusConfiguration();
        configuration.GetSection("MessageBus").Bind(config);
        
        return services.AddMessageBus(config, assemblies);
    }

    /// <summary>
    /// Registers the Message Bus with custom configuration
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The message bus configuration</param>
    /// <param name="assemblies">Assemblies to scan for message handlers</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddMessageBus(
        this IServiceCollection services,
        MessageBusConfiguration configuration,
        params Assembly[] assemblies)
    {
        // Register configuration
        services.AddSingleton(configuration);
        
        // Register MessageBus as singleton
        services.AddSingleton<IMessageBus, MessageBus>();
        
        // Register message handlers from assemblies
        if (assemblies?.Length > 0)
        {
            services.RegisterMessageHandlers(assemblies);
        }
        else
        {
            // If no assemblies provided, scan the calling assembly
            services.RegisterMessageHandlers(Assembly.GetCallingAssembly());
        }
        
        return services;
    }

    /// <summary>
    /// Registers the Message Bus with configuration action
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configureOptions">Action to configure the message bus</param>
    /// <param name="assemblies">Assemblies to scan for message handlers</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddMessageBus(
        this IServiceCollection services,
        Action<MessageBusConfiguration> configureOptions,
        params Assembly[] assemblies)
    {
        var configuration = new MessageBusConfiguration();
        configureOptions(configuration);
        
        return services.AddMessageBus(configuration, assemblies);
    }

    /// <summary>
    /// Registers message handlers from the specified assemblies
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="assemblies">Assemblies to scan for handlers</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection RegisterMessageHandlers(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        if (assemblies == null || assemblies.Length == 0)
        {
            assemblies = new[] { Assembly.GetCallingAssembly() };
        }

        foreach (var assembly in assemblies)
        {
            RegisterHandlersFromAssembly(services, assembly);
        }

        return services;
    }

    private static void RegisterHandlersFromAssembly(IServiceCollection services, Assembly assembly)
    {
        var handlerTypes = assembly.GetTypes()
            .Where(type => type.IsClass && !type.IsAbstract)
            .Where(type => type.GetInterfaces().Any(IsHandlerInterface))
            .ToList();

        foreach (var handlerType in handlerTypes)
        {
            var handlerInterfaces = handlerType.GetInterfaces()
                .Where(IsHandlerInterface)
                .ToList();

            foreach (var handlerInterface in handlerInterfaces)
            {
                services.AddScoped(handlerInterface, handlerType);
            }
        }
    }

    private static bool IsHandlerInterface(Type type)
    {
        if (!type.IsGenericType)
            return false;

        var genericTypeDefinition = type.GetGenericTypeDefinition();
        
        return genericTypeDefinition == typeof(IEventHandler<>) ||
               genericTypeDefinition == typeof(ICommandHandler<>) ||
               genericTypeDefinition == typeof(IMessageHandler<>);
    }

    /// <summary>
    /// Adds Message Bus with minimal configuration for development
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="assemblies">Assemblies to scan for message handlers</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddMessageBusForDevelopment(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        var config = new MessageBusConfiguration
        {
            Host = "localhost",
            Port = 5672,
            Username = "guest",
            Password = "guest",
            VirtualHost = "/",
            ServiceName = "Development"
        };

        return services.AddMessageBus(config, assemblies);
    }
}