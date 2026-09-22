namespace CleanBase.Domain.Common.Errors;

public static partial class Errors
{
    public static class Profile
    {
        public static Error NotFound => Error.NotFound(
            code: "Profile.NotFound",
            description: "Profile not found."
        );

        public static Error UpdateFailed(string description) => Error.Failure(
            code: "Profile.UpdateFailed",
            description: description
        );
    }
}
