namespace BuildingBlocks.MessageBus;

/// <summary>
/// Interface principal do Message Bus para comunicação assíncrona entre serviços
/// </summary>
public interface IMessageBus : IDisposable
{
    /// <summary>
    /// Publica um evento para todos os subscribers interessados
    /// </summary>
    /// <typeparam name="T">Tipo do evento</typeparam>
    /// <param name="event">Evento a ser publicado</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Task representando a operação assíncrona</returns>
    Task PublishEventAsync<T>(T @event, CancellationToken cancellationToken = default) 
        where T : class, IEvent;

    /// <summary>
    /// Envia um comando para um serviço específico
    /// </summary>
    /// <typeparam name="T">Tipo do comando</typeparam>
    /// <param name="command">Comando a ser enviado</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Task representando a operação assíncrona</returns>
    Task SendCommandAsync<T>(T command, CancellationToken cancellationToken = default) 
        where T : class, ICommand;

    /// <summary>
    /// Subscreve a eventos de um tipo específico
    /// </summary>
    /// <typeparam name="T">Tipo do evento</typeparam>
    /// <param name="handler">Handler para processar o evento</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Task representando a operação assíncrona</returns>
    Task SubscribeEventAsync<T>(Func<T, CancellationToken, Task> handler, CancellationToken cancellationToken = default) 
        where T : class, IEvent;

    /// <summary>
    /// Subscreve a comandos de um tipo específico
    /// </summary>
    /// <typeparam name="T">Tipo do comando</typeparam>
    /// <param name="handler">Handler para processar o comando</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Task representando a operação assíncrona</returns>
    Task SubscribeCommandAsync<T>(Func<T, CancellationToken, Task> handler, CancellationToken cancellationToken = default) 
        where T : class, ICommand;

    /// <summary>
    /// Configura um exchange no RabbitMQ
    /// </summary>
    /// <param name="exchangeName">Nome do exchange</param>
    /// <param name="exchangeType">Tipo do exchange (direct, topic, fanout, headers)</param>
    /// <param name="durable">Se o exchange deve ser durável</param>
    /// <param name="autoDelete">Se o exchange deve ser deletado automaticamente</param>
    /// <returns>Task representando a operação assíncrona</returns>
    Task DeclareExchangeAsync(string exchangeName, string exchangeType = "topic", bool durable = true, bool autoDelete = false);

    /// <summary>
    /// Configura uma fila no RabbitMQ
    /// </summary>
    /// <param name="queueName">Nome da fila</param>
    /// <param name="durable">Se a fila deve ser durável</param>
    /// <param name="exclusive">Se a fila é exclusiva para esta conexão</param>
    /// <param name="autoDelete">Se a fila deve ser deletada automaticamente</param>
    /// <returns>Task representando a operação assíncrona</returns>
    Task DeclareQueueAsync(string queueName, bool durable = true, bool exclusive = false, bool autoDelete = false);

    /// <summary>
    /// Vincula uma fila a um exchange com uma routing key
    /// </summary>
    /// <param name="queueName">Nome da fila</param>
    /// <param name="exchangeName">Nome do exchange</param>
    /// <param name="routingKey">Routing key para o binding</param>
    /// <returns>Task representando a operação assíncrona</returns>
    Task BindQueueAsync(string queueName, string exchangeName, string routingKey);

    /// <summary>
    /// Inicia o consumo de mensagens (deve ser chamado após configurar subscribers)
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Task representando a operação assíncrona</returns>
    Task StartConsumingAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Para o consumo de mensagens
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Task representando a operação assíncrona</returns>
    Task StopConsumingAsync(CancellationToken cancellationToken = default);
}