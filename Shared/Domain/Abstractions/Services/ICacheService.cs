namespace Domain.Abstractions.Services;

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
    /// <summary>
    /// Инвалидация кеша
    /// </summary>
    /// <param name="cacheVersionKey">ключ версии кэша</param>
    /// <returns></returns>
    Task InvalidateCache(string cacheVersionKey);
    /// <summary>
    /// Получить текущую версию кэша по ключу
    /// </summary>
    /// <param name="cacheVersionKey">ключ версии кэша</param>
    /// <returns></returns>
    Task<long> GetCurrentCacheVersion(string cacheVersionKey);
    /// <summary>
    /// Сгенерировать ключ по префиксу и параметрам
    /// </summary>
    /// <param name="prefix">префикс ключа</param>
    /// <param name="cacheVersion">версия кэша</param>
    /// <param name="parameters">параметры</param>
    /// <returns></returns>
    string GenerateCacheKey(string prefix, long cacheVersion, object parameters);
}