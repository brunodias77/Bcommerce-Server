using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Mediator;

/// <summary>
/// Implementação principal do Mediator
/// Utiliza o ServiceProvider para resolver handlers dinamicamente
/// </summary>
public class Mediator : IMediator
{
    private readonly IServiceProvider _serviceProvider;

    public Mediator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <inheritdoc />
    public async Task SendAsync<TRequest>(TRequest request, CancellationToken cancellationToken = default)
        where TRequest : IRequest
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        var handler = _serviceProvider.GetService<IRequestHandler<TRequest>>();
        
        if (handler == null)
            throw new InvalidOperationException($"Handler não encontrado para o tipo {typeof(TRequest).Name}");

        await handler.HandleAsync(request, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        var requestType = request.GetType();
        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, typeof(TResponse));
        
        var handler = _serviceProvider.GetService(handlerType);
        
        if (handler == null)
            throw new InvalidOperationException($"Handler não encontrado para o tipo {requestType.Name}");

        var method = handlerType.GetMethod(nameof(IRequestHandler<IRequest<TResponse>, TResponse>.HandleAsync));
        
        if (method == null)
            throw new InvalidOperationException($"Método HandleAsync não encontrado no handler {handlerType.Name}");

        var task = (Task<TResponse>)method.Invoke(handler, new object[] { request, cancellationToken })!;
        
        return await task;
    }

    /// <inheritdoc />
    public async Task PublishAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        if (notification == null)
            throw new ArgumentNullException(nameof(notification));

        var handlers = _serviceProvider.GetServices<INotificationHandler<TNotification>>();
        
        if (!handlers.Any())
            return; // Não há handlers registrados, mas isso não é um erro para notifications

        var tasks = handlers.Select(handler => handler.HandleAsync(notification, cancellationToken));
        
        await Task.WhenAll(tasks);
    }
}