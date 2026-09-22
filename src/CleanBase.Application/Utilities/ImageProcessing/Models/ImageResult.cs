namespace CleanBase.Application.Utilities.ImageProcessing.Models;

public record ImageResult(byte[] Data, string ContentType = "image/webp", int Width = 0, int Height = 0)
{
    private const string WebpContentType = "image/webp";

    public static ImageResult Create(byte[] data, int width, int height)
        => new(data, WebpContentType, width, height);
}