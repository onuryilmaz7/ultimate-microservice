using System.Security.Cryptography.X509Certificates;

namespace EventBus.Base;

public class EventBusConfig
{
    public int ConnectionTryCount { get; set; } = 5;

    public string DefaultTopicName { get; set; } = "UltimateEventBus";

    public string EventBusConnectionString { get; set; } = string.Empty;

    public string SubscriberClientAppName { get; set; } = string.Empty;

    public string EventNamePrefix { get; set; } = string.Empty;

    public string EventNameSuffix { get; set; } = "IntegrationEvent";

    public EventBusType EventBusType { get; set; } = EventBusType.RabbitMQ;

    public object Connection { get; set; }

    public bool DeleteEventPrefix => !String.IsNullOrEmpty(EventNamePrefix);
    public bool DeleteEventSuffix => !String.IsNullOrEmpty(EventNameSuffix);
}

public enum EventBusType : byte
{
    RabbitMQ = 0,
    AzureServiceBus = 1
}