namespace PracticalWork.Library.Abstractions.MessageBroker;

/// <summary>
/// Издатель, отправляет сообщения в очередь
/// </summary>
public interface IRabbitMqPublisher
{
    /// <summary>
    /// Опубликовать сообщение
    /// </summary>
    /// <param name="exchange">название обменника</param>
    /// <param name="routingKey">ключ маршрутизации</param>
    /// <param name="message">сообщение</param>
    /// <param name="cancellationToken">токен отмены</param>
    /// <typeparam name="T">тип сообщения</typeparam>
    /// <returns>удалось ли отправить сообщение</returns>
    Task<bool> PublishAsync<T>(string exchange, string routingKey,
        T message, CancellationToken cancellationToken = default);
}