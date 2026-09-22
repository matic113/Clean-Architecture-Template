using FluentEmail.Core;
using CleanBase.Application.Common.Interfaces;
using CleanBase.Domain.Identity;
using Microsoft.Extensions.Logging;

namespace CleanBase.Infrastructure.Email;

public class EmailService(IFluentEmail email, ILogger<EmailService> logger) : IEmailService
{
    public async Task<bool> SendOtpEmailAsync(string toEmail, string code, OtpPurpose purpose)
    {
        try
        {
            var (subject, templateFile) = purpose switch
            {
                OtpPurpose.EmailVerification => ("Verify your email - CleanBase", "EmailVerification.cshtml"),
                OtpPurpose.AccountDeletion => ("Confirm your account deletion - CleanBase", "AccountDeletion.cshtml"),
                _ => ("Reset your password - CleanBase", "PasswordReset.cshtml"),
            };

            var templatePath = Path.Combine(AppContext.BaseDirectory, templateFile);

            var response = await email
                .To(toEmail)
                .Subject(subject)
                .UsingTemplateFromFile(templatePath, new { Code = code })
                .SendAsync();

            if (!response.Successful)
            {
                logger.LogError("Failed to send OTP email to {Email}. Errors: {Errors}",
                    toEmail, string.Join(", ", response.ErrorMessages));
            }

            return response.Successful;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception occurred while sending OTP email to {Email}", toEmail);
            return false;
        }
    }
}