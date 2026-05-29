using System.Collections.Concurrent;
using Domain.Options;
using MessageBroker.Configuration.Abstractions;
using MessageBroker.Rabbit.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace MessageBroker.Rabbit.Utils;

internal class RabbitMqChannelPool: IRabbitMqChannelPool, IInitializable
{
    private readonly ILogger<RabbitMqChannelPool> _logger;
    private readonly ConcurrentBag<IChannel> _channelPool = [];
    private readonly ConnectionFactory _factory;
    private IConnection? _connection;
    private readonly string _appName;
    private readonly int _maxPoolSize;
    private readonly SemaphoreSlim _channelLock;
    private readonly List<IChannel> _consumersChannels = new();
    
    public bool IsInitialized { get; set; }
    
    public RabbitMqChannelPool(IOptionsMonitor<RabbitOptions> rabbitOptionsMonitor, 
        ILogger<RabbitMqChannelPool> logger)
    {
        var options = rabbitOptionsMonitor.CurrentValue;
        var factory = new ConnectionFactory
        {
            HostName = options.Host,
            UserName = options.User,
            Password = options.Password,
            Port = options.Port,
        };
        _appName = options.AppName;
        _logger = logger;
        _factory = factory;
        _maxPoolSize = options.MaxPoolSize;
        _channelLock = new SemaphoreSlim(_maxPoolSize, _maxPoolSize);
    }

    public async Task InitializeAsync()
    {
        _connection = await _factory.CreateConnectionAsync(_appName);
        _connection.ConnectionShutdownAsync += (_, ea) =>
        {
            _logger.LogWarning("RabbitMQ connection shutdown: {Reason}", ea.ReplyText);
            return Task.CompletedTask;
        };
    }

    public async Task<IChannel> GetChannelAsync(CancellationToken cancellationToken = default)
    {
        await _channelLock.WaitAsync(cancellationToken);
        try
        {
            if (_channelPool.TryTake(out var channel))
            {
                if (channel is { IsOpen: true })
                {
                    return channel;
                }
            }
            if (_connection is null) throw new NullReferenceException(nameof(_connection));
            channel = await _connection.CreateChannelAsync(cancellationToken:cancellationToken);
            channel.ChannelShutdownAsync += (_, ea) =>
            {
                _logger.LogError("RabbitMQ channel shutdown: {Reason}", ea.ReplyText);
                return Task.CompletedTask;
            };
            return channel;
        }
        catch (Exception)
        {
            _channelLock.Release();
            throw;
        }
    }

    public async Task<IChannel> GetChannelForConsumerAsync()
    {
        if (_connection is null) throw new NullReferenceException(nameof(_connection));
        var channel = await _connection.CreateChannelAsync();
        _consumersChannels.Add(channel);
        channel.ChannelShutdownAsync += (_, ea) =>
        {
            _logger.LogError("RabbitMQ channel shutdown: {Reason}", ea.ReplyText);
            return Task.CompletedTask;
        };
        return channel;
    }

    public void ReturnChannel(IChannel channel)
    {
        try
        {
            if (!channel.IsOpen)
            {
                _logger.LogDebug("Канал закрыт, не возвращаем в пул");
                channel.Dispose();
            }
            if (_channelPool.Count < _maxPoolSize)
            {
                _channelPool.Add(channel);
                _logger.LogDebug("Канал возвращен в пул. Всего: {Count}",
                    _channelPool.Count);
            }
            else
            {
                _logger.LogDebug("Пул переполнен, закрываем канал");
                channel.Dispose();
            }
        }
        finally
        {
            _channelLock.Release(1);
        }
    }
    
    public void Dispose()
    {
        foreach (var channel in _channelPool)
        {
            if (!channel.IsOpen) continue;
            channel.Dispose();
        }
        foreach (var consumersChannel in _consumersChannels
                     .Where(consumersChannel => consumersChannel.IsOpen))
        {
            consumersChannel.Dispose();
        }
        if (_connection is not null && _connection.IsOpen)
        {
            _connection.Dispose();
        }
        _channelPool.Clear();
        _channelLock.Dispose();
    }
}