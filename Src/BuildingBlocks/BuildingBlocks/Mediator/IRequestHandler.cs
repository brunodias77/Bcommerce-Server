namespace BuildingBlocks.Mediator;

/// <summary>
/// Interface para handlers de Commands (sem retorno)
/// </summary>
/// <typeparam name="TRequest">Tipo do Command</typeparam>
public interface IRequestHandler<in TRequest>
    where TRequest : IRequest
{
    /// <summary>
    /// Executa o Command de forma assíncrona
    /// </summary>
    /// <param name="request">O Command a ser executado</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Task representando a operação assíncrona</returns>
    Task HandleAsync(TRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface para handlers de Queries (com retorno)
/// </summary>
/// <typeparam name="TRequest">Tipo da Query</typeparam>
/// <typeparam name="TResponse">Tipo do retorno</typeparam>
public interface IRequestHandler<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>
    /// Executa a Query de forma assíncrona
    /// </summary>
    /// <param name="request">A Query a ser executada</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Task contendo o resultado da Query</returns>
    Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken = default);
}