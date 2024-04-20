using EventBus.Base.Events;

namespace EventBus.UnitTests.Events.Events;

public class OrderCreatedIntegrationEvent(int id) : IntegrationEvent
{
    public int Id { get; set; } = id;
}