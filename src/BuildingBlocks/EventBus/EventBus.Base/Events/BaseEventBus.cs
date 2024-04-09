using System.Threading.Tasks.Dataflow;
using EventBus.Base.Abstraction;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace EventBus.Base.Events;

public class BaseEventBus : IEventBus
{
    public readonly IServiceProvider ServiceProvider;
    public readonly IEventBusSubscriptionManager SubsManager;

    public EventBusConfig EventBusConfig { get; set; }

    public BaseEventBus(EventBusConfig eventBusConfig, IServiceProvider serviceProvider)
    {
        EventBusConfig = eventBusConfig;
        ServiceProvider = serviceProvider;
    }

    public virtual string ProcessEventName(string eventName)
    {
        if (EventBusConfig is not null)
        {
            if (EventBusConfig.DeleteEventPrefix)
                eventName = eventName.TrimStart(EventBusConfig.EventNamePrefix.ToArray());

            if (EventBusConfig.DeleteEventSuffix)
                eventName = eventName.TrimEnd(EventBusConfig.EventNameSuffix.ToArray());
        }
        
        return eventName;
    }

    public virtual string GetSubName(string eventName)
    {
        return $"{EventBusConfig?.SubscriberClientAppName}.{ProcessEventName(eventName)}";
    }

    public virtual void Dispose()
    {
        EventBusConfig = null;
    }

    public async Task<bool> ProcessEventAsync(string eventName, string message)
    {
        eventName = ProcessEventName(eventName);

        bool processed = false;

        if (SubsManager.HasSubscriptionForEvent(eventName))
        {
            var subscriptions = SubsManager.GetHandlersForEvent(eventName);

            using (var scope = ServiceProvider.CreateScope())
            {
                foreach (var subscription in subscriptions)
                {
                    var handler = ServiceProvider.GetService(subscription.HandlerType);
                    if(handler is null) continue;

                    var eventType = SubsManager.GetEventTypeByName($"{EventBusConfig?.EventNamePrefix}{eventName}{EventBusConfig?.EventNameSuffix}");
                    var integrationEvent = JsonConvert.DeserializeObject(message, eventType);

                    var concreteType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);
                    await (Task)concreteType?.GetMethod("Handle").Invoke(handler, new[] { integrationEvent });

                }
            }

            processed = true;
        }

        return processed;
    }
    
    public  virtual void Publish(IntegrationEvent @event)
    {
        throw new NotImplementedException();
    }

    public virtual void Subscribe<T, TH>() where T : IntegrationEvent where TH : IIntegrationEventHandler<T>
    {
        throw new NotImplementedException();
    }

    public virtual void UnSubscribe<T, TH>() where T : IntegrationEvent where TH : IIntegrationEventHandler<T>
    {
        throw new NotImplementedException();
    }
}