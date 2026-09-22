namespace CleanBase.Domain.Common.Errors;

public static partial class Errors
{
    public static class Identity
    {
        public static Error ExternalInfoMissing => Error.Validation(
            code: "Identity.ExternalInfoMissing",
            description: "External login info missing."
        );

        public static Error UserNotFound => Error.NotFound(
            code: "Identity.UserNotFound",
            description: "Account not found."
        );

        public static Error InvalidRedirectUri => Error.Validation(
            code: "Identity.InvalidRedirectUri",
            description: "Redirect URI is not in the allowed whitelist."
        );

        public static Error RedirectUriMissing => Error.Validation(
            code: "Identity.RedirectUriMissing",
            description: "Redirect URI is required but not provided."
        );

        public static Error EmailAlreadyExists => Error.Conflict(
            code: "Identity.EmailAlreadyExists",
            description: "An account with this email already exists."
        );

        public static Error InvalidCredentials => Error.Validation(
            code: "Identity.InvalidCredentials",
            description: "Invalid email or password."
        );

        public static Error EmailNotConfirmed => Error.Validation(
            code: "Identity.EmailNotConfirmed",
            description: "Please verify your email with the OTP code sent to your inbox."
        );

        public static Error RegistrationFailed(string description) => Error.Failure(
            code: "Identity.RegistrationFailed",
            description: description
        );

        public static Error UserUpdateFailed => Error.Failure(
            code: "Identity.UserUpdateFailed",
            description: "User update failed. Please try again."
        );

        public static Error PasswordValidationFailed(string errors) => Error.Validation(
            code: "Identity.PasswordValidationFailed",
            description: errors
        );

        public static Error InvalidOtp => Error.Validation(
            code: "Identity.InvalidOtp",
            description: "Invalid OTP code. Please check the code and try again."
        );

        public static Error EmailAlreadyVerified => Error.Validation(
            code: "Identity.EmailAlreadyVerified",
            description: "Email is already verified."
        );

        public static Error OtpExpired => Error.Validation(
            code: "Identity.OtpExpired",
            description: "OTP code has expired. Please request a new one."
        );

        public static Error OtpResendCooldown(int remainingSeconds) => Error.Validation(
            code: "Identity.OtpResendCooldown",
            description: $"Please wait {remainingSeconds} seconds before requesting a new OTP."
        );

        public static Error InvalidRefreshToken => Error.Validation(
            code: "Identity.InvalidRefreshToken",
            description: "Invalid refresh token."
        );

        public static Error ExpiredRefreshToken => Error.Validation(
            code: "Identity.ExpiredRefreshToken",
            description: "Refresh token has expired. Please login again."
        );

        public static Error RevokedRefreshToken => Error.Validation(
            code: "Identity.RevokedRefreshToken",
            description: "Refresh token has been revoked."
        );

        public static Error InvalidGoogleIdToken => Error.Validation(
            code: "Identity.InvalidGoogleIdToken",
            description: "Invalid Google ID token. Please try logging in again."
        );

        public static Error UnexpectedGoogleIdTokenError => Error.Validation(
            code: "Identity.UnexpectedGoogleIdTokenError",
            description: "Something went wrong during Google ID token validation."
        );

        public static Error InvalidAppleIdToken => Error.Validation(
            code: "Identity.InvalidAppleIdToken",
            description: "Invalid Apple ID token. Please try logging in again."
        );

        public static Error UnexpectedAppleIdTokenError => Error.Validation(
            code: "Identity.UnexpectedAppleIdTokenError",
            description: "Something went wrong during Apple ID token validation."
        );

        public static Error AppleEmailNotProvided => Error.Validation(
            code: "Identity.AppleEmailNotProvided",
            description: "Apple did not provide an email address."
        );

        public static Error AppleEmailNotVerified => Error.Validation(
            code: "Identity.AppleEmailNotVerified",
            description: "Apple email address is not verified."
        );

        public static Error AccountPendingDeletion => Error.Conflict(
            code: "Identity.AccountPendingDeletion",
            description: "This account is scheduled for deletion. Please contact support to restore it."
        );

        public static Error NotPendingDeletion => Error.Validation(
            code: "Identity.NotPendingDeletion",
            description: "This account is not scheduled for deletion."
        );
    }
}
