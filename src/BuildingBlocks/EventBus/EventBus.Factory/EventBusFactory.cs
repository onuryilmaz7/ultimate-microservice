using EventBus.AzureServiceBus;
using EventBus.Base;

public static class EventBusFactory
{
    public static IEventBus Create(EventBusConfig config, IServiceProvider serviceProvider)
    {
        return config.EventBusType switch
        {
            EventBusType.AzureServiceBus => new EventBusServiceBus(config, serviceProvider),
            _ => new EventBusRabbitMQ(config, serviceProvider)
        };
    }
}