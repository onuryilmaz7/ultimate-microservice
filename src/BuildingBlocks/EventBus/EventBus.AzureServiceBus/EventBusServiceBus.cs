using System.Text;
using EventBus.Base;
using EventBus.Base.Events;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Management;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace EventBus.AzureServiceBus;

public class EventBusServiceBus : BaseEventBus
{
    private ITopicClient? _topicClient;
    private ManagementClient? _managementClient;
    private ILogger _logger;
    public EventBusServiceBus(EventBusConfig eventBusConfig, IServiceProvider serviceProvider) : base(eventBusConfig,
        serviceProvider)
    {
        _logger = serviceProvider.GetRequiredService<ILogger<EventBusServiceBus>>();
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
            var subsClient = CreateSubscriptionClientIfNotExist(eventName);

            RegisterSubscriptionClientMessageHandler(subsClient);
        }

        _logger.LogInformation("Subscribing to event: {EventName} with {EventHandler}", eventName, typeof(TH).Name);

        SubsManager.AddSubscription<T, TH>();
    }

    public override void UnSubscribe<T, TH>()
    {
        var eventName = typeof(T).Name;
        
        try
        {
            var subsClient = CreateSubscriptionClient(eventName);

            subsClient.RemoveRuleAsync(eventName).GetAwaiter().GetResult();    


        }
        catch (MessagingEntityNotFoundException)
        {
            _logger.LogWarning("The messaging entity {eventName} not found", eventName);
        }

        _logger.LogInformation("Unsubscribing from event {EventName}", eventName);

        SubsManager.RemoveSubscription<T, TH>();
    }

    public override void Dispose()
    {
        base.Dispose();


        _topicClient?.CloseAsync().GetAwaiter().GetResult();
        _managementClient?.CloseAsync().GetAwaiter().GetResult();

        _topicClient = null;
        _managementClient = null;
    }

    private void RegisterSubscriptionClientMessageHandler(ISubscriptionClient subscriptionClient)
    {
        subscriptionClient.RegisterMessageHandler(async (message, token) =>
        {
            var eventName = $"{message.Label}";
            var messageData = Encoding.UTF8.GetString(message.Body);

            if (await ProcessEventAsync(ProcessEventName(eventName), messageData))
            {
                await subscriptionClient.CompleteAsync(message.SystemProperties.LockToken);
            }


        },
        new MessageHandlerOptions(exceptionReceivedHandler: ExceptionReceiveHandler)
        {
            MaxConcurrentCalls = 10,
            AutoComplete = false
        });
    }

    private Task ExceptionReceiveHandler(ExceptionReceivedEventArgs exceptionReceivedEventArgs)
    {
        var ex = exceptionReceivedEventArgs.Exception;
        var context = exceptionReceivedEventArgs.ExceptionReceivedContext;

        _logger.LogError(ex, $"ERROR handling message: {ex.Message} - Context: {context}");

        return Task.CompletedTask;
    }

    private ISubscriptionClient CreateSubscriptionClientIfNotExist(string eventName)
    {
        var subsClient = CreateSubscriptionClient(eventName);

        var exist = _managementClient.SubscriptionExistsAsync(EventBusConfig.DefaultTopicName, GetSubName(eventName)).GetAwaiter().GetResult();

        if (!exist)
        {
            _managementClient.CreateSubscriptionAsync(EventBusConfig.DefaultTopicName, GetSubName(eventName)).GetAwaiter().GetResult();
            RemoveDefaultRule(subsClient);
        }

        CreateRuleIfNotExist(ProcessEventName(eventName), subsClient);

        return subsClient;
    }


    private void CreateRuleIfNotExist(string eventName, ISubscriptionClient subscriptionClient)
    {
        bool ruleExists;

        try
        {
            var rule = _managementClient.GetRuleAsync(EventBusConfig.DefaultTopicName, eventName, eventName).GetAwaiter().GetResult();
            ruleExists = rule != null;

            if (!ruleExists)
            {
                subscriptionClient.AddRuleAsync(new RuleDescription()
                {
                    Name = eventName,
                    Filter = new CorrelationFilter()
                    {
                        Label = eventName
                    }
                }).GetAwaiter().GetResult();
            }

        }
        catch (MessagingEntityNotFoundException)
        {
            ruleExists = false;
        }

    }
    private void RemoveDefaultRule(SubscriptionClient subsClient)
    {
        try
        {
            subsClient.RemoveRuleAsync(RuleDescription.DefaultRuleName).GetAwaiter().GetResult();
        }
        catch (MessagingEntityNotFoundException)
        {
            _logger.LogWarning("The message entity {DefaultRuleName} Could not be found", RuleDescription.DefaultRuleName);
        }
    }

    private SubscriptionClient CreateSubscriptionClient(string eventName)
    {
        return new(EventBusConfig.EventBusConnectionString, EventBusConfig.DefaultTopicName, GetSubName(eventName));
    }
}