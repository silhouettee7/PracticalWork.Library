using System.Collections.Concurrent;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PracticalWork.Library.MessageBroker.Configuration.Abstractions;
using PracticalWork.Library.MessageBroker.Rabbit.Abstractions;
using RabbitMQ.Client;

namespace PracticalWork.Library.MessageBroker.Rabbit.Utils;

public class RabbitMQChannelPool: IRabbitMQChannelPool, IInitializable
{
    private readonly ILogger<RabbitMQChannelPool> _logger;
    private readonly ConcurrentBag<IChannel> _channelPool = [];
    private readonly ConnectionFactory _factory;
    private IConnection? _connection;
    private readonly string _appName;
    private readonly int _maxPoolSize;
    private readonly SemaphoreSlim _channelLock;
    private List<IChannel> _consumersChannels = new();
    
    public RabbitMQChannelPool(IConfiguration configuration, 
        ILogger<RabbitMQChannelPool> logger)
    {
        var factory = new ConnectionFactory
        {
            HostName = configuration["RabbitMQ:HostName"] ?? "localhost",
            UserName = configuration["RabbitMQ:UserName"] ?? "guest",
            Password = configuration["RabbitMQ:Password"] ?? "guest",
            Port = configuration.GetValue("RabbitMQ:Port", 5672),
        };
        _appName = configuration["RabbitMQ:AppName"] ?? Guid.NewGuid().ToString();
        _logger = logger;
        _factory = factory;
        _maxPoolSize = configuration.GetValue("RabbitMQ:MaxChannelPoolSize", 10);
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

    public bool IsInit { get; set; }

    public async Task<IChannel> GetChannelAsync()
    {
        await _channelLock.WaitAsync();
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
            channel = await _connection.CreateChannelAsync();
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