using CleanBase.Domain.Identity;

namespace CleanBase.Application.Common.Interfaces;

public interface IEmailService
{
    Task<bool> SendOtpEmailAsync(string toEmail, string code, OtpPurpose purpose);
}