using EventBus.UnitTests.Events.Events;

namespace EventBus.UnitTests.Events.EventHandlers;

public class OrderCreatedIntegrationEventHandler : IIntegrationEventHandler<OrderCreatedIntegrationEvent>
{
    public Task Handle(OrderCreatedIntegrationEvent @event)
    {
        Console.WriteLine($"Handle method work with id: {@event.Id}");
        return Task.CompletedTask;
    }
}