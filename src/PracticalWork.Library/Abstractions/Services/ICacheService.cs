namespace PracticalWork.Library.Abstractions.Services;

/// <summary>
/// Сервис для управления кешированием
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Прочитать значение по ключу
    /// </summary>
    /// <param name="key">ключ кеша</param>
    /// <typeparam name="T">тип десериализуемого объекта</typeparam>
    /// <returns>десериализованный объект</returns>
    Task<T> GetAsync<T>(string key);
    /// <summary>
    /// Записать значение по ключу 
    /// </summary>
    /// <param name="key">ключ кеша</param>
    /// <param name="value">значение</param>
    /// <param name="expiry">срок хранения</param>
    /// <typeparam name="T">тип сериализуемого объекта</typeparam>
    /// <returns></returns>
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null);
    /// <summary>
    /// Удалить значение по ключу
    /// </summary>
    /// <param name="key">ключ кеша</param>
    /// <returns>удачно или нет</returns>
    Task<bool> RemoveAsync(string key);
    /// <summary>
    /// Проверить существование значения по ключу
    /// </summary>
    /// <param name="key">ключ кеша</param>
    /// <returns>существует или нет</returns>
    Task<bool> ExistsAsync(string key);
}