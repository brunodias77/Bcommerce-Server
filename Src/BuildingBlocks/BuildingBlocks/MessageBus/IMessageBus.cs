namespace BuildingBlocks.MessageBus;

public interface IMessageBus : IDisposable
{
    /// <summary>
    /// Publica um evento (pub/sub - múltiplos consumidores)
    /// </summary>
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) 
        where TEvent : class, IEvent;

    /// <summary>
    /// Envia um comando (queue - único consumidor)
    /// </summary>
    Task SendAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default) 
        where TCommand : class, ICommand;

    /// <summary>
    /// Inicia consumo de mensagens (chame após registrar handlers)
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Para consumo de mensagens
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken = default);
}