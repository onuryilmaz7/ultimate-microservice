using System.Net.Sockets;
using System.Numerics;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using EventBus.Base;
using EventBus.Base.Events;
using Newtonsoft.Json;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

public class EventBusRabbitMQ : BaseEventBus
{

    private RabbitMQPersistentConnection _persistentConnection;
    private readonly IConnectionFactory? _connectionFactory;
    private readonly IModel _consumerChannel;

    public EventBusRabbitMQ(EventBusConfig eventBusConfig, IServiceProvider serviceProvider) : base(eventBusConfig, serviceProvider)
    {
        if (EventBusConfig.Connection is not null)
        {
            var connJson = JsonConvert.SerializeObject(EventBusConfig, new JsonSerializerSettings()
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            });

            _connectionFactory = JsonConvert.DeserializeObject<ConnectionFactory>(connJson)
                ?? throw new ArgumentNullException(nameof(connJson));
        }

        _persistentConnection = new RabbitMQPersistentConnection(_connectionFactory, 6);

        _consumerChannel = CreateConsumerChannel();

        SubsManager.OnEventRemoved += OnSubsManager_OnEventRemoved;
    }



    public override void Publish(IntegrationEvent @event)
    {
        if (!_persistentConnection.IsConnected)
        {
            _persistentConnection.TryConnect();
        }

        var policy = Policy.Handle<BrokerUnreachableException>()
                .Or<SocketException>()
                .WaitAndRetry(EventBusConfig.ConnectionTryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
                {
                    // logging
                });

        var eventName = @event.GetType().Name;
        eventName = ProcessEventName(eventName);

        _consumerChannel.ExchangeDeclare(exchange: EventBusConfig.DefaultTopicName, type: "direct");

        var message = JsonConvert.SerializeObject(@event);
        var body = Encoding.UTF8.GetBytes(message);

        policy.Execute(() =>
        {
            var properties = _consumerChannel.CreateBasicProperties();
            properties.DeliveryMode = 2; // persistent

            _consumerChannel.QueueDeclare(queue: GetSubName(eventName),  // ensure queue exist before publish
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            _consumerChannel.BasicPublish(exchange: EventBusConfig.DefaultTopicName,
                    routingKey: eventName,
                    mandatory: true,
                    basicProperties: properties,
                    body: body);
        });
    }

    public override void Subscribe<T, TH>()
    {
        var eventName = typeof(T).Name;
        eventName = ProcessEventName(eventName);

        if (!SubsManager.HasSubscriptionForEvent(eventName))
        {
            _consumerChannel.QueueDeclare(queue: GetSubName(eventName),
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            _consumerChannel.QueueBind(queue: GetSubName(eventName),
                    exchange: EventBusConfig.DefaultTopicName,
                    routingKey: eventName);

            SubsManager.AddSubscription<T, TH>();

            StartBasicConsume(eventName);

        }
    }

    public override void UnSubscribe<T, TH>()
    {
        SubsManager.RemoveSubscription<T, TH>();
    }
    private IModel CreateConsumerChannel()
    {
        if (!_persistentConnection.IsConnected)
        {
            _persistentConnection.TryConnect();
        }

        var channel = _persistentConnection.CreateModel();

        channel.ExchangeDeclare(exchange: EventBusConfig.DefaultTopicName, type: "direct");

        return channel;
    }

    private void StartBasicConsume(string eventName)
    {
        if (_consumerChannel != null)
        {
            var consumer = new EventingBasicConsumer(_consumerChannel);

            consumer.Received += Consumer_Received;

            _consumerChannel.BasicConsume(queue: GetSubName(eventName),
                    autoAck: false,
                    consumer: consumer);
        }
    }

    private async void Consumer_Received(object? sender, BasicDeliverEventArgs e)
    {
        var eventName = e.RoutingKey;
        eventName = ProcessEventName(eventName);
        var message = Encoding.UTF8.GetString(e.Body.Span);

        try
        {
            await ProcessEventAsync(eventName, message);
        }
        catch (System.Exception)
        {
            // logging;   
            throw;
        }

        _consumerChannel.BasicAck(e.DeliveryTag, multiple: false);

        throw new NotImplementedException();
    }

    private void OnSubsManager_OnEventRemoved(object? sender, string eventName)
    {
        eventName = ProcessEventName(eventName);
        if (!_persistentConnection.IsConnected)
        {
            _persistentConnection.TryConnect();
        }

        // Exchang'den geldigi zaman queueya bu datayi gonderme (unbind)
        _consumerChannel.QueueUnbind(queue: GetSubName(eventName),
                exchange: EventBusConfig.DefaultTopicName,
                routingKey: eventName);

        if (SubsManager.IsEmpty)
        {
            _consumerChannel.Close();
        }

    }
}