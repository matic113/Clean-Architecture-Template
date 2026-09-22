using CleanBase.Application.Utilities.ImageProcessing.Models;

namespace CleanBase.Application.Utilities.ImageProcessing;

public static class PresetHelper
{
    public static (int Width, int Height)? GetDimensions(ImagePreset preset)
    {
        return preset switch
        {
            ImagePreset.Size1600 => (1600, 1600),
            ImagePreset.Size1000 => (1000, 1000),
            ImagePreset.Size600 => (600, 600),
            ImagePreset.Size400 => (400, 400),
            ImagePreset.Size200 => (200, 200),
            _ => null
        };
    }

    public static IEnumerable<ImagePreset> GetPresetsToProcess()
    {
        return new[]
        {
            ImagePreset.Size1600,
            ImagePreset.Size1000,
            ImagePreset.Size600,
            ImagePreset.Size400,
            ImagePreset.Size200
        };
    }
}