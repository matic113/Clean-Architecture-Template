using CleanBase.Application.Utilities.ImageProcessing;
using CleanBase.Application.Utilities.ImageProcessing.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace CleanBase.Infrastructure.Utilities.ImageProcessing;

public class ImageProcessor : IImageProcessor
{
    private const int DefaultQuality = 80;

    public async Task<ImageResult> ProcessAsync(Stream input, ImagePreset preset)
    {
        var dimensions = PresetHelper.GetDimensions(preset);
        if (dimensions == null)
        {
            return await ProcessAsync(input, new ImageOptions(0, 0, DefaultQuality));
        }

        var (width, height) = dimensions.Value;
        return await ProcessAsync(input, new ImageOptions(width, height, DefaultQuality));
    }

    public async Task<ImageResult> ProcessAsync(Stream input, ImageOptions options)
    {
        using var image = await Image.LoadAsync(input);

        int width = options.Width;
        int height = options.Height;

        if (width > 0 || height > 0)
        {
            width = width > 0 ? width : image.Width;
            height = height > 0 ? height : image.Height;

            image.Mutate(x => x.Resize(
                new ResizeOptions
                {
                    Size = new Size(width, height),
                    Mode = ResizeMode.Max
                }));
        }

        var encoder = new WebpEncoder
        {
            Quality = options.Quality,
        };

        using var outputStream = new MemoryStream();
        await image.SaveAsync(outputStream, encoder);
        var outputData = outputStream.ToArray();

        return ImageResult.Create(outputData, image.Width, image.Height);
    }
}