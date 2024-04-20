using EventBus.Base;
using EventBus.Base.Abstraction;
using EventBus.UnitTests.Events.EventHandlers;
using EventBus.UnitTests.Events.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace EventBus.UnitTests;

[TestClass]
public class EventBusTests
{
    private readonly ServiceCollection _services;

    public EventBusTests()
    {
        _services = new ServiceCollection();
        _services.AddLogging(configure => configure.AddConsole());
    }

    [TestMethod]
    public void Subscribe_Event_On_RabbitMQ_Test()
    {
        _services.AddSingleton<IEventBus>(sp =>
        {
            return EventBusFactory.Create(GetRabbitMQConfig(), sp);
        });

        var sp = _services.BuildServiceProvider();

        var eventBus = sp.GetRequiredService<IEventBus>();

        eventBus.Subscribe<OrderCreatedIntegrationEvent, OrderCreatedIntegrationEventHandler>();
        // eventBus.UnSubscribe<OrderCreatedIntegrationEvent, OrderCreatedIntegrationEventHandler>();
    }

    [TestMethod]
    public void Subscribe_Event_On_Azure_Test()
    {
        _services.AddSingleton<IEventBus>(sp =>
        {
           
            return EventBusFactory.Create(GetAzureConfig(), sp);
        });

        var sp = _services.BuildServiceProvider();

        var eventBus = sp.GetRequiredService<IEventBus>();

        eventBus.Subscribe<OrderCreatedIntegrationEvent, OrderCreatedIntegrationEventHandler>();
        eventBus.UnSubscribe<OrderCreatedIntegrationEvent, OrderCreatedIntegrationEventHandler>();
    }

    [TestMethod]
    public void Send_Message_To_RabbitMQ_Test()
    {
        _services.AddSingleton<IEventBus>(sp =>
        {
            return EventBusFactory.Create(GetRabbitMQConfig(), sp);
        });
        
        var sp = _services.BuildServiceProvider();

        var eventBus = sp.GetRequiredService<IEventBus>();
        
        eventBus.Publish(new OrderCreatedIntegrationEvent(1));
    }

    private EventBusConfig GetRabbitMQConfig()
    {
       return  new()
        {
            ConnectionTryCount = 5,
            SubscriberClientAppName = "EventBusUnitTest",
            DefaultTopicName = "UltimateEventBus",
            EventBusType = EventBusType.RabbitMQ,
            EventNameSuffix = "IntegrationEvent",
            // Connection = new ConnectionFactory()
            // {
            //     HostName = "localhost",
            //     Port = 15672,
            //     UserName = "guest",
            //     Password = "guest"
            // }
        };
    }

    
    private EventBusConfig GetAzureConfig()
    {
        return  new()
        {
            ConnectionTryCount = 5,
            SubscriberClientAppName = "EventBusUnitTest",
            DefaultTopicName = "UltimateEventBus",
            EventBusType = EventBusType.AzureServiceBus,
            EventNameSuffix = "IntegrationEvent",
            EventBusConnectionString = "" // need connection string for AzureServiceBus here
        };
    }
}