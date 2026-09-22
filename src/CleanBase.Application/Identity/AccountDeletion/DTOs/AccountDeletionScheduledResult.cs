namespace CleanBase.Application.Identity.AccountDeletion.DTOs;

/// <summary>Returned after a confirmed deletion — when the account will be permanently purged.</summary>
public record AccountDeletionScheduledResult(DateTime PurgeScheduledAt);
