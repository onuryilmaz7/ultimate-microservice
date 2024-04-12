using System.Net.Sockets;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

public class RabbitMQPersistentConnection : IDisposable
{

    private IConnection _connection;
    private IConnectionFactory _connectionFactory;
    private object lock_object = new object();
    private int _retryCount;
    private bool _disposed;
    public RabbitMQPersistentConnection(IConnectionFactory connectionFactory, int retryCount = 5)
    {
        _connectionFactory = connectionFactory;
        _retryCount = retryCount;
    }
    public bool IsConnected => _connection != null && _connection.IsOpen;


    public IModel CreateModel()
    {
        return _connection.CreateModel();
    }

    public void Dispose()
    {
        _connection.Dispose();
        _disposed = true;
    }

    public bool TryConnect()
    {
        lock (lock_object)
        {
            var policy = Policy.Handle<SocketException>()
            .Or<BrokerUnreachableException>()
            .WaitAndRetry(_retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
            {

            });

            policy.Execute(() =>
            {
                _connection = _connectionFactory.CreateConnection();
            });

            if (IsConnected)
            {
                // log
                _connection.ConnectionShutdown += Connection_ConnectionShutdown;
                _connection.ConnectionBlocked += Connection_ConnectionBlocked;
                _connection.ConnectionUnblocked += Connection_ConnectionUnblocked;

                return true;
            }
            return false;
        }
    }

    private void Connection_ConnectionBlocked(object? sender, ConnectionBlockedEventArgs e)
    {
        if (_disposed) return;
        TryConnect();

    }

    private void Connection_ConnectionUnblocked(object? sender, EventArgs e)
    {
        if (_disposed) return;
        TryConnect();

    }

    private void Connection_ConnectionShutdown(object? sender, ShutdownEventArgs e)
    {
        // log ConnectionShutDown
        if (_disposed) return;
        TryConnect();
    }
}