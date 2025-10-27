namespace BuildingBlocks.MessageBus;

public abstract record Event : IEvent
{
    public Guid MessageId { get; init; } = Guid.NewGuid();
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public abstract string EventName { get; }
}
