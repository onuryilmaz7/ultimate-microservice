using Newtonsoft.Json;

namespace EventBus.Base.Events;

public class IntegrationEvent
{
    public IntegrationEvent()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
    }
    [JsonConstructor]
    public IntegrationEvent(Guid id , DateTime createdAt)
    {
        Id = id;
        CreatedAt = createdAt;
    }
    
    [JsonProperty]
    public Guid Id { get; private set; }
    [JsonProperty]
    public DateTime CreatedAt { get; private set; }
}