using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PracticalWork.Library.Abstractions.Services;
using PracticalWork.Library.Options;

namespace PracticalWork.Library.Data.Minio;

public static class Entry
{
    /// <summary>
    /// Регистрация зависимостей для хранилища документов
    /// </summary>
    public static IServiceCollection AddMinioFileStorage(this IServiceCollection serviceCollection, IConfiguration configuration)
    {
        serviceCollection.Configure<MinioOptions>(configuration.GetSection("App:Minio"));
        serviceCollection.AddScoped<IFileStorageService, MinioService>();
        return serviceCollection;
    }
}
