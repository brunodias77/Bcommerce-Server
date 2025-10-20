namespace BuildingBlocks.Mediator;


/// <summary>
/// Interface base para Commands (sem retorno)
/// </summary>
public interface IRequest
{
}

/// <summary>
/// Interface base para Queries (com retorno)
/// </summary>
/// <typeparam name="TResponse">Tipo do retorno esperado</typeparam>
public interface IRequest<out TResponse> : IRequest
{
}