using CleanBase.Application.Utilities.ImageProcessing.Models;

namespace CleanBase.Application.Utilities.ImageProcessing;

public interface IImageProcessor
{
    Task<ImageResult> ProcessAsync(Stream input, ImagePreset preset);
    Task<ImageResult> ProcessAsync(Stream input, ImageOptions options);
}