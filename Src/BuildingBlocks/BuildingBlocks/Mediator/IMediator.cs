namespace BuildingBlocks.Mediator;

/// <summary>
/// Interface principal do Mediator
/// Responsável por rotear Commands, Queries e Events para seus respectivos handlers
/// </summary>
public interface IMediator
{
    /// <summary>
    /// Envia um Command para ser processado (sem retorno)
    /// </summary>
    /// <typeparam name="TRequest">Tipo do Command</typeparam>
    /// <param name="request">O Command a ser enviado</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Task representando a operação assíncrona</returns>
    Task SendAsync<TRequest>(TRequest request, CancellationToken cancellationToken = default)
        where TRequest : IRequest;

    /// <summary>
    /// Envia uma Query para ser processada (com retorno)
    /// </summary>
    /// <typeparam name="TResponse">Tipo do retorno esperado</typeparam>
    /// <param name="request">A Query a ser enviada</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Task contendo o resultado da Query</returns>
    Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publica um Event/Notification para todos os handlers registrados
    /// </summary>
    /// <typeparam name="TNotification">Tipo do Event/Notification</typeparam>
    /// <param name="notification">O Event/Notification a ser publicado</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Task representando a operação assíncrona</returns>
    Task PublishAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification;
}