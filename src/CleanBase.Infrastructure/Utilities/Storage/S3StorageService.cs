using CleanBase.Application.Utilities.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;

namespace CleanBase.Infrastructure.Utilities.Storage;

public class S3StorageService(
    IMinioClient minioClient,
    IOptions<S3Options> s3Options,
    ILogger<S3StorageService> logger
) : IStorageService
{
    private readonly S3Options _options = s3Options.Value;

    public async Task<FileUploadDto> CreatePresignedPutUrlAsync(
        string? bucket = null,
        string folder = "",
        string fileName = "",
        int expiryMinutes = 15)
    {
        var fileExtension = Path.GetExtension(fileName);
        var objectName = $"{folder}/{Guid.NewGuid()}{fileExtension}".TrimStart('/');
        var bucketName = ResolveBucket(objectName, bucket);
        var args = new PresignedPutObjectArgs()
            .WithBucket(bucketName)
            .WithObject(objectName)
            .WithExpiry(expiryMinutes * 60);

        var presignedUrl = await minioClient.PresignedPutObjectAsync(args);

        var response = new FileUploadDto(
            ObjectKey: objectName,
            UploadUrl: new Uri(presignedUrl),
            Method: "PUT",
            ExpiresAt: DateTime.UtcNow.AddMinutes(expiryMinutes)
        );

        return response;
    }

    public async Task DeleteAsync(string fileName, string? bucketName = null)
    {
        try
        {
            var bucket = ResolveBucket(fileName, bucketName);
            var removeArgs = new RemoveObjectArgs()
                .WithBucket(bucket)
                .WithObject(fileName);

            if (fileName != "temp/test.jpg") // Avoid deleting the test file
                await minioClient.RemoveObjectAsync(removeArgs);

        }
        catch (Exception e)
        {
            logger.LogError(
                e,
                "Error deleting object {ObjectKey}",
                fileName
            );
            throw;
        }
    }

    public Task<string> GetPresignedUrlAsync(string fileName, string? bucketName = null, int expiryMinutes = 60)
    {
        var bucket = ResolveBucket(fileName, bucketName);

        var args = new PresignedGetObjectArgs()
            .WithBucket(bucket)
            .WithObject(fileName)
            .WithExpiry(expiryMinutes * 60);

        return minioClient.PresignedGetObjectAsync(args);
    }

    public string? GetPublicObjectUrl(string fileName, string? bucketName = null)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return null;

        var objectUrl = $"{_options.PublicBaseUrl}/{fileName}";
        return objectUrl;
    }

    public async Task<bool> MoveAsync(
        string sourceKey,
        string? sourceBucket = null,
        string destinationKey = "",
        string? destinationBucket = null)
    {
        try
        {
            // Copy the object to the new location
            var sourceBucketName = ResolveBucket(sourceKey, sourceBucket);
            var destinationBucketName = ResolveBucket(destinationKey, destinationBucket);

            var copyArgs = new CopyObjectArgs()
                .WithCopyObjectSource(
                    new CopySourceObjectArgs()
                        .WithBucket(sourceBucketName)
                        .WithObject(sourceKey)
                )
                .WithBucket(destinationBucketName)
                .WithObject(destinationKey);

            await minioClient.CopyObjectAsync(copyArgs);

            // Remove the original object
            var removeArgs = new RemoveObjectArgs()
                .WithBucket(sourceBucketName)
                .WithObject(sourceKey);
            if (sourceKey != "temp/test.jpg") // Avoid deleting the test file
                await minioClient.RemoveObjectAsync(removeArgs);
        }
        catch (Exception e)
        {
            logger.LogError(
                e,
                "Error moving object from {SourceKey} to {DestinationKey}",
                sourceKey,
                destinationKey
            );
            return false;
        }

        return true;
    }

    public async Task<string> UploadAsync(
        Stream stream,
        string fileName,
        string contentType,
        string? bucketName = null)
    {
        try
        {
            var bucket = ResolveBucket(fileName, bucketName);

            var args = new PutObjectArgs()
                .WithBucket(bucket)
                .WithObject(fileName)
                .WithStreamData(stream)
                .WithObjectSize(stream.Length)
                .WithContentType(contentType);

            await minioClient.PutObjectAsync(args);
            return fileName; // Just return the object key, not bucket/key
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error uploading object {ObjectKey} to bucket {Bucket}",
                fileName, bucketName);
            throw;
        }
    }

    public async Task<Stream> DownloadAsync(string fileName, string? bucketName = null)
    {
        var memoryStream = new MemoryStream();
        try
        {
            var bucket = ResolveBucket(fileName, bucketName);
            var args = new GetObjectArgs()
                .WithBucket(bucket)
                .WithObject(fileName)
                .WithCallbackStream(stream => stream.CopyTo(memoryStream));

            await minioClient.GetObjectAsync(args);
            memoryStream.Position = 0;
            return memoryStream;
        }
        catch (Exception e)
        {
            await memoryStream.DisposeAsync();
            logger.LogError(e, "Error Downloading object {ObjectKey} from bucket {Bucket}",
                fileName, bucketName);
            throw;
        }
    }

    public async Task<bool> ObjectExistsAsync(string fileName, string? bucketName = null)
    {
        try
        {
            var bucket = ResolveBucket(fileName, bucketName);
            var args = new StatObjectArgs()
                .WithBucket(bucket)
                .WithObject(fileName);

            await minioClient.StatObjectAsync(args);
            return true;
        }
        catch (Minio.Exceptions.ObjectNotFoundException)
        {
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking object existence for {ObjectKey}", fileName);
            return false;
        }
    }

    /// <summary>
    /// Resolves the target bucket. An explicit bucket wins; otherwise the object key's leading
    /// segment decides: <c>public/…</c> ⇒ public bucket, anything else ⇒ private bucket (safe default).
    /// </summary>
    private string ResolveBucket(string objectKey, string? bucketName)
    {
        if (!string.IsNullOrWhiteSpace(bucketName))
            return bucketName;

        return objectKey.StartsWith($"{StorageKeys.PublicPrefix}/", StringComparison.OrdinalIgnoreCase)
            ? _options.PublicBucketName
            : _options.PrivateBucketName;
    }
}