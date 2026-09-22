namespace CleanBase.Application.Identity.Authentication.DTOs;

public record JwtResult(string Token, DateTime ExpiresAt);