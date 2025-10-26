namespace BuildingBlocks.MessageBus;

public abstract record Command : ICommand
{
    public Guid MessageId { get; init; } = Guid.NewGuid();
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public abstract string CommandName { get; }
}