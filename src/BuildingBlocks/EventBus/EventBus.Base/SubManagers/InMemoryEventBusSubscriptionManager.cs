using EventBus.Base.Abstraction;
using EventBus.Base.Events;

namespace EventBus.Base.SubManagers;

public class InMemoryEventBusSubscriptionManager : IEventBusSubscriptionManager
{
    private readonly Dictionary<string, List<SubscribtionInfo>> _handlers;
    private readonly List<Type> _eventTypes;

    public event EventHandler<string>? OnEventRemoved;
    public Func<string, string> _eventNameGetter;

    public InMemoryEventBusSubscriptionManager(Func<string, string> eventNameGetter)
    {
        _eventNameGetter = eventNameGetter;
        _eventTypes = new List<Type>();
        _handlers = new Dictionary<string, List<SubscribtionInfo>>();
    }

    public bool IsEmpty => !_handlers.Keys.Any();

    public void Clear() => _handlers.Clear();

    public void AddSubscription<T, TH>() where T : IntegrationEvent where TH : IIntegrationEventHandler<T>
    {
        string eventName = GetEventKey<T>();

        AddSubscription(typeof(TH), eventName);

        if (!_eventTypes.Contains(typeof(T)))
        {
            _eventTypes.Add(typeof(T));
        }
    }

    private void AddSubscription(Type handlerType, string eventName)
    {
        if (!HasSubscriptionForEvent(eventName))
        {
            _handlers.Add(eventName, new List<SubscribtionInfo>());
        }

        if (_handlers[eventName].Any(handler => handler.HandlerType == handlerType))
        {
            throw new ArgumentException($"Handler Type {handlerType.Name} already registered for '{eventName}'",
                nameof(handlerType));
        }
        
        _handlers[eventName].Add(SubscribtionInfo.Typed(handlerType));
        
    }

    public void RemoveSubscription<T, TH>() where T : IntegrationEvent where TH : IIntegrationEventHandler<T>
    {
        var handlerToRemove = FindSubscriptionToRemove<T, TH>();
        throw new NotImplementedException();
    }

    private SubscribtionInfo? FindSubscriptionToRemove<T, TH>() where T : IntegrationEvent where TH : IIntegrationEventHandler<T>
    {
        string eventName = GetEventKey<T>();

        return FindSubscriptionToRemove(eventName, typeof(TH));
    }

    private SubscribtionInfo? FindSubscriptionToRemove(string eventName, Type handlerType)
    {
        if (!HasSubscriptionForEvent(eventName))
        {
            return null;
        }

        return _handlers[eventName].SingleOrDefault(handler => handler.HandlerType == handlerType);
    }

    public bool HasSubscriptionForEvent<T>() where T : IntegrationEvent
    {
        var eventKey = GetEventKey<T>();

        return HasSubscriptionForEvent(eventKey);
    }

    public bool HasSubscriptionForEvent(string eventName) => _handlers.ContainsKey(eventName);


    public Type? GetEventTypeByName(string eventName) =>
        _eventTypes.SingleOrDefault(eventType => eventType.Name == eventName);
    

    public IEnumerable<SubscribtionInfo> GetHandlersForEvent<T>() where T : IntegrationEvent
    {
        string eventName = GetEventKey<T>();
        
        return GetHandlersForEvent(eventName);
    }

    public IEnumerable<SubscribtionInfo> GetHandlersForEvent(string eventName) => _handlers[eventName];

    private void RaiseOnEventRemoved(string eventName)
    {
        var handler = OnEventRemoved;
        handler?.Invoke(this, eventName);
    }
    public string GetEventKey<T>()
    {
        string eventName = typeof(T).Name;

        return _eventNameGetter(eventName);
    }
}