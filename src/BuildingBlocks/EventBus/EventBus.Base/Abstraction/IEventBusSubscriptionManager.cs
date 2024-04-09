using EventBus.Base.Events;

namespace EventBus.Base.Abstraction;

public interface IEventBusSubscriptionManager
{
    public bool IsEmpty { get; }
    
    event EventHandler<string> OnEventRemoved;
    
    void AddSubscription<T, TH>() where T : IntegrationEvent where TH : IIntegrationEventHandler<T>;
    
    void RemoveSubscription<T, TH>() where T : IntegrationEvent where TH : IIntegrationEventHandler<T>;

    bool HasSubscriptionForEvent<T>() where T : IntegrationEvent;

    bool HasSubscriptionForEvent(string eventName);

    Type? GetEventTypeByName(string eventName);

    void Clear();

    IEnumerable<SubscribtionInfo> GetHandlersForEvent<T>() where T : IntegrationEvent;

    IEnumerable<SubscribtionInfo> GetHandlersForEvent(string eventName);

    string GetEventKey<T>(); 


}