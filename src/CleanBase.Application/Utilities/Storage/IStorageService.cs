namespace CleanBase.Application.Utilities.Storage;

public interface IStorageService
{
    Task<string> UploadAsync(Stream stream, string fileName, string contentType, string? bucketName = null);
    Task<Stream> DownloadAsync(string fileName, string? bucketName = null);
    Task DeleteAsync(string fileName, string? bucketName = null);

    string? GetPublicObjectUrl(string fileName, string? bucketName = null);
    Task<string> GetPresignedUrlAsync(string fileName, string? bucketName = null, int expiryMinutes = 60);

    Task<FileUploadDto> CreatePresignedPutUrlAsync(
        string? bucket = null,
        string folder = "",
        string fileName = "",
        int expiryMinutes = 15
        );

    Task<bool> MoveAsync(string sourceKey, string? sourceBucket = null, string destinationKey = "",
        string? destinationBucket = null);

    Task<bool> ObjectExistsAsync(string fileName, string? bucketName = null);
}