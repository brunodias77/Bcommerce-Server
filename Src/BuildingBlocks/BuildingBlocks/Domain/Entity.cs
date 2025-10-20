using BuildingBlocks.Mediator;

namespace BuildingBlocks.Domain;

public abstract class Entity
{
    private readonly List<INotification> _domainEvents = new();

    public Guid Id { get; protected set; }
    public DateTime CreatedAt { get; protected set; }
    public DateTime UpdatedAt { get; protected set; } 
    
    protected Entity() 
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public IReadOnlyCollection<INotification> DomainEvents => _domainEvents.AsReadOnly();

    protected void AddDomainEvent(INotification eventItem) => _domainEvents.Add(eventItem);
    public void ClearDomainEvents() => _domainEvents.Clear();
    protected void MarkAsModified() => UpdatedAt = DateTime.UtcNow;
}