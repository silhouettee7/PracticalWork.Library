namespace PracticalWork.Library.Abstractions.MessageBroker;

/// <summary>
/// Издатель, отправляет сообщения в очередь
/// </summary>
public interface IRabbitMQPublisher
{
    /// <summary>
    /// Опубликовать сообщение
    /// </summary>
    /// <param name="exchange">название обменника</param>
    /// <param name="routingKey">ключ маршрутизации</param>
    /// <param name="message">сообщение</param>
    /// <typeparam name="T">тип сообщения</typeparam>
    /// <returns>удалось ли отправить сообщение</returns>
    Task<bool> PublishAsync<T>(string exchange, string routingKey, T message);
}