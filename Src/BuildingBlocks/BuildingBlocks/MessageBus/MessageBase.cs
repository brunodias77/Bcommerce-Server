namespace BuildingBlocks.MessageBus;

/// <summary>
/// Classe base abstrata para implementação de mensagens
/// </summary>
public abstract class MessageBase : IMessage
{
    public Guid MessageId { get; protected set; }
    public DateTime CreatedAt { get; protected set; }
    public abstract string MessageType { get; }
    public virtual string Version { get; protected set; } = "1.0";

    protected MessageBase()
    {
        MessageId = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
    }

    protected MessageBase(Guid messageId, DateTime createdAt)
    {
        MessageId = messageId;
        CreatedAt = createdAt;
    }
}