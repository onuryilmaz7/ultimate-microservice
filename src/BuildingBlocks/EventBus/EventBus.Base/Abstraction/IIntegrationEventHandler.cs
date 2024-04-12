
using EventBus.Base.Events;

public interface IIntegrationEventHandler<TIntegrationEvent> : IntegrationEventHandler where TIntegrationEvent : IntegrationEvent
{
    Task Handle(TIntegrationEvent @event);

}

public interface IntegrationEventHandler
{
    
}