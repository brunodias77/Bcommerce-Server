namespace BuildingBlocks.MessageBus;

/// <summary>
/// Interface base para todas as mensagens (eventos e comandos)
/// </summary>
public interface IMessage
{
    Guid MessageId { get; }
    DateTime CreatedAt { get; }
}