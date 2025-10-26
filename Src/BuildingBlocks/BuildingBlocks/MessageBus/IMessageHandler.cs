namespace BuildingBlocks.MessageBus;

/// <summary>
/// Handler único para processar mensagens
/// </summary>
public interface IMessageHandler<in TMessage> where TMessage : IMessage
{
    Task HandleAsync(TMessage message, CancellationToken cancellationToken = default);
}