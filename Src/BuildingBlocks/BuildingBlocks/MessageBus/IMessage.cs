namespace BuildingBlocks.MessageBus;

/// <summary>
/// Interface base para todas as mensagens do sistema
/// </summary>
public interface IMessage
{
    /// <summary>
    /// Identificador único da mensagem
    /// </summary>
    Guid MessageId { get; }
    
    /// <summary>
    /// Timestamp de quando a mensagem foi criada
    /// </summary>
    DateTime CreatedAt { get; }
    
    /// <summary>
    /// Tipo da mensagem para roteamento
    /// </summary>
    string MessageType { get; }
    
    /// <summary>
    /// Versão da mensagem para compatibilidade
    /// </summary>
    string Version { get; }
}