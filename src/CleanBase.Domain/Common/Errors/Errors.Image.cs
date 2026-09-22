namespace CleanBase.Domain.Common.Errors;

public static partial class Errors
{
    public static class Image
    {
        public static Error Invalid => Error.Validation(
            code: "Image.Invalid",
            description: "The uploaded file could not be read as an image. Upload a valid image file."
        );
    }
}
