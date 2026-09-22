using CleanBase.Application.Identity.AccountDeletion.DTOs;

namespace CleanBase.Application.Identity.AccountDeletion.Interfaces;

public interface IAccountDeletionService
{
    /// <summary>Sends an email OTP to confirm a self-service account deletion. Non-enumerating.</summary>
    Task<ErrorOr<Success>> RequestDeletionAsync(RequestAccountDeletionRequest request);

    /// <summary>Validates the OTP and locks the account into the 30-day pending-deletion window.</summary>
    Task<ErrorOr<AccountDeletionScheduledResult>> ConfirmDeletionAsync(ConfirmAccountDeletionRequest request);
}
