namespace CleanBase.Application.Identity.Authentication.DTOs;

public record AuthTokenResult(string AccessToken, string RefreshToken, DateTime ExpiresAt);