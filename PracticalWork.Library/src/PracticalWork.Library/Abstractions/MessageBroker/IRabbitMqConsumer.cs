namespace PracticalWork.Library.Abstractions.MessageBroker;

/// <summary>
/// Потребитель очереди сообщений
/// </summary>
public interface IRabbitMqConsumer
{
    /// <summary>
    /// Подписаться на очередь, получать сообщения и обрабатывать
    /// </summary>
    /// <param name="queueName">название очереди</param>
    /// <returns>задача</returns>
    Task StartConsuming(string queueName);
    /// <summary>
    /// Остановить потребление
    /// </summary>
    /// <returns>задача</returns>
    Task StopConsuming();
}