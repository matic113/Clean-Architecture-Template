namespace CleanBase.Application.Utilities.Storage;

public record FileUploadDto(string ObjectKey, Uri UploadUrl, string Method, DateTime ExpiresAt);