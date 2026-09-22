using System.ComponentModel.DataAnnotations;

namespace CleanBase.Infrastructure.Utilities.Storage;

public class S3Options
{
    public const string SectionName = "S3";
    [Required] public required string PublicBaseUrl { get; set; }
    [Required] public required string Endpoint { get; set; }
    [Required] public required string AccessKey { get; set; }
    [Required] public required string SecretKey { get; set; }
    [Required] public required string Region { get; set; }
    [Required] public required string PublicBucketName { get; set; }
    [Required] public required string PrivateBucketName { get; set; }
    [Required] public required bool EnableSsl { get; set; }
}