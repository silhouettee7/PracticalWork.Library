using Domain.Abstractions.Services;
using Domain.Options;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using Minio.DataModel.ILM;
using Minio.Exceptions;

namespace Data.Minio;

public class MinioService : IFileStorageService
{
    private readonly IMinioClient _minioClient;
    private readonly MinioOptions _minioOptions;

    public MinioService(IOptionsMonitor<MinioOptions> minioOptions)
    {
        _minioOptions = minioOptions.CurrentValue;
        _minioClient = new MinioClient()
            .WithEndpoint(_minioOptions.Endpoint)
            .WithCredentials(_minioOptions.AccessKey, _minioOptions.SecretKey)
            .WithSSL(false)
            .Build();
    }

    public async Task UploadFileAsync(string bucket, string fileName, Stream fileStream, string contentType,
        CancellationToken cancellationToken = default)
    {
        var bucketExists = await _minioClient.BucketExistsAsync(
            new BucketExistsArgs().WithBucket(bucket), cancellationToken);
        if (!bucketExists)
        {
            await _minioClient
                .MakeBucketAsync(new MakeBucketArgs()
                    .WithBucket(bucket), cancellationToken);
        }

        fileStream.Position = 0;
        await _minioClient.PutObjectAsync(new PutObjectArgs()
            .WithBucket(bucket)
            .WithObject(fileName)
            .WithStreamData(fileStream)
            .WithObjectSize(fileStream.Length)
            .WithContentType(contentType), cancellationToken);
    }

    public async Task<string> GetFileLinkAsync(string bucket, string fileName, CancellationToken cancellationToken = default)
    {
        await CheckExistingAsync(bucket, fileName);
        var expiryInSeconds = _minioOptions.ExpInSeconds;

        var args = new PresignedGetObjectArgs()
            .WithBucket(bucket)
            .WithObject(fileName)
            .WithExpiry(expiryInSeconds);

        var url = await _minioClient.PresignedGetObjectAsync(args);

        return url;
    }

    public async Task SetBucketFilesLifeTimeAsync(string bucket, DateTime deleteDate, string prefix, CancellationToken cancellationToken = default)
    {
        var rule = new LifecycleRule
        {
            ID = $"ExpireOn{deleteDate:yyyy-MM-dd}",
            Status = "Enabled",
            Filter = new RuleFilter
            {
                Prefix = prefix
            },
            Expiration = new Expiration(deleteDate)
        };
        
        var lifecycleConfig = new LifecycleConfiguration(
            new List<LifecycleRule> { rule }
        );
        
        await _minioClient.SetBucketLifecycleAsync(
            new SetBucketLifecycleArgs()
                .WithBucket(bucket)
                .WithLifecycleConfiguration(lifecycleConfig), cancellationToken);
    }

    private async Task CheckExistingAsync(string bucket, string fileName)
    {
        try
        {
            var obj = await _minioClient.StatObjectAsync(new StatObjectArgs()
                .WithBucket(bucket)
                .WithObject(fileName));
        }   
        catch (ObjectNotFoundException)
        {
            throw new FileNotFoundException($"Файл {fileName} не существует");
        }
        catch (BucketNotFoundException)
        {
            throw new FileNotFoundException($"Бакет {bucket} не существует");
        }
    }
}