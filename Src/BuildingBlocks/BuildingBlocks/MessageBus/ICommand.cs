namespace BuildingBlocks.MessageBus;

/// <summary>
/// Interface para comandos que devem ser enviados através do message bus
/// Comandos representam uma intenção de fazer algo no sistema
/// </summary>
public interface ICommand : IMessage
{
    /// <summary>
    /// Identificador do usuário que iniciou o comando
    /// </summary>
    Guid? UserId { get; }
    
    /// <summary>
    /// Contexto de correlação para rastreamento
    /// </summary>
    string? CorrelationId { get; }
    
    /// <summary>
    /// Serviço de destino que deve processar o comando
    /// </summary>
    string TargetService { get; }
    
    /// <summary>
    /// Prioridade do comando (0 = baixa, 10 = alta)
    /// </summary>
    int Priority { get; }
}