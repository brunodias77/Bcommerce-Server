namespace BuildingBlocks.MessageBus;

/// <summary>
/// Interface para eventos de domínio que devem ser publicados no message bus
/// Eventos representam algo que já aconteceu no sistema
/// </summary>
public interface IEvent : IMessage
{
    /// <summary>
    /// Identificador da entidade que gerou o evento
    /// </summary>
    Guid AggregateId { get; }
    
    /// <summary>
    /// Versão do agregado quando o evento foi gerado
    /// </summary>
    int AggregateVersion { get; }
    
    /// <summary>
    /// Contexto ou origem do evento (ex: "UserService", "OrderService")
    /// </summary>
    string Source { get; }
}