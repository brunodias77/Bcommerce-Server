namespace BuildingBlocks.MessageBus;

/// <summary>
/// Evento de domínio - algo que já aconteceu
/// Exemplo: ProductCreatedEvent, OrderPlacedEvent
/// </summary>
public interface IEvent : IMessage
{
    string EventName { get; }
}