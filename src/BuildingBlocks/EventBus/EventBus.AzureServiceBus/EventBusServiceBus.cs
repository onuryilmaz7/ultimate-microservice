using System.Text;
using EventBus.Base;
using EventBus.Base.Events;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Management;
using Newtonsoft.Json;

namespace EventBus.AzureServiceBus;

public class EventBusServiceBus : BaseEventBus
{
    private ITopicClient _topicClient;
    private ManagementClient _managementClient;

    public EventBusServiceBus(EventBusConfig eventBusConfig, IServiceProvider serviceProvider) : base(eventBusConfig,
        serviceProvider)
    {
        _managementClient = new ManagementClient(EventBusConfig.EventBusConnectionString);
    }

    private ITopicClient CreateTopicClient()
    {
        if (_topicClient == null || _topicClient.IsClosedOrClosing)
        {
            _topicClient = new TopicClient(EventBusConfig.EventBusConnectionString, EventBusConfig.DefaultTopicName,
                RetryPolicy.Default);
        }

        if (!_managementClient.TopicExistsAsync(EventBusConfig.DefaultTopicName).GetAwaiter().GetResult())
        {
            _managementClient.CreateTopicAsync(EventBusConfig.DefaultTopicName).GetAwaiter().GetResult();
        }

        return _topicClient;
    }

    public override void Publish(IntegrationEvent @event)
    {
        var eventName = @event.GetType().Name;
        eventName = ProcessEventName(eventName);

        var eventStr = JsonConvert.SerializeObject(@event);
        var bodyArr = Encoding.UTF8.GetBytes(eventStr);
        
        var message = new Message()
        {
            MessageId = Guid.NewGuid().ToString(),
            Body = bodyArr,
            Label = eventName
        };
        
        _topicClient.SendAsync(message).GetAwaiter().GetResult();
    }

    public override void Subscribe<T, TH>()
    {
        var eventName = typeof(T).Name;
        eventName = ProcessEventName(eventName);


        if (!SubsManager.HasSubscriptionForEvent(eventName))
        {
            var subsClient = CreateSubscriptionClient(eventName);
        }

    }

    private SubscriptionClient CreateSubscriptionClient(string eventName)
    {
        return new(EventBusConfig.EventBusConnectionString, EventBusConfig.DefaultTopicName, GetSubName(eventName));
    }
}