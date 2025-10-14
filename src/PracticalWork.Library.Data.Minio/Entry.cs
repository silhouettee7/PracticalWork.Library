using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace PracticalWork.Library.Data.Minio;

public static class Entry
{
    /// <summary>
    /// Регистрация зависимостей для хранилища документов
    /// </summary>
    public static IServiceCollection AddMinioFileStorage(this IServiceCollection serviceCollection, IConfiguration configuration)
    {
        var connectionString = configuration["App:Minio:MinioFileStorageConnection"];

        // Реализация подключения к Minio и сервисов

        return serviceCollection;
    }
}
