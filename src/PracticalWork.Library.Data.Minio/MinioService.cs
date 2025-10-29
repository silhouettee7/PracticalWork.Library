using System.Runtime.InteropServices.JavaScript;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using PracticalWork.Library.Abstractions.Services;

namespace PracticalWork.Library.Data.Minio;

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

    public async Task UploadFileAsync(string fileName, Stream fileStream, string contentType,
        CancellationToken cancellationToken = default)
    {
        var bucketExists = await _minioClient.BucketExistsAsync(
            new BucketExistsArgs().WithBucket(_minioOptions.BucketName), cancellationToken);
        if (!bucketExists)
        {
            await _minioClient
                .MakeBucketAsync(new MakeBucketArgs()
                    .WithBucket(_minioOptions.BucketName), cancellationToken);
        }

        fileStream.Position = 0;
        await _minioClient.PutObjectAsync(new PutObjectArgs()
            .WithBucket(_minioOptions.BucketName)
            .WithObject(fileName)
            .WithStreamData(fileStream)
            .WithObjectSize(fileStream.Length)
            .WithContentType(contentType), cancellationToken);
    }

    public async Task DeleteFileAsync(string fileName, CancellationToken cancellationToken = default)
    {
        var existResult = await FileExistsAsync(fileName, cancellationToken);
        await _minioClient.RemoveObjectAsync(new RemoveObjectArgs()
            .WithBucket(_minioOptions.BucketName)
            .WithObject(fileName), cancellationToken);
    }

    public async Task<bool> FileExistsAsync(string fileName, CancellationToken cancellationToken = default)
    {
        await _minioClient.StatObjectAsync(new StatObjectArgs()
            .WithBucket(_minioOptions.BucketName)
            .WithObject(fileName), cancellationToken);

        return true;
    }

    public async Task<string> GetFileUrlAsync(string fileName, CancellationToken cancellationToken = default)
    {
        var expiryInSeconds = _minioOptions.ExpInSeconds;

        var args = new PresignedGetObjectArgs()
            .WithBucket(_minioOptions.BucketName)
            .WithObject(fileName)
            .WithExpiry(expiryInSeconds);

        var url = await _minioClient.PresignedGetObjectAsync(args);

        return url;
    }
}