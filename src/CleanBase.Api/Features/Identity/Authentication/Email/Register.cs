using CleanBase.Application.Identity.Authentication.DTOs;
using CleanBase.Application.Identity.Authentication.Interfaces;

namespace CleanBase.Api.Features.Identity.Authentication.Email;

public class Register
{
    public sealed record RegisterRequestDto
    {
        public string Email { get; init; } = string.Empty;
        public string Password { get; init; } = string.Empty;
        public string? FirstName { get; init; }
        public string? LastName { get; init; }
        public string? PhoneNumber { get; init; }

        public class Validator : Validator<RegisterRequestDto>
        {
            public Validator()
            {
                RuleFor(x => x.Email)
                    .NotEmpty().WithMessage("Email is required.")
                    .EmailAddress().WithMessage("Please provide a valid email address.");

                RuleFor(x => x.Password)
                    .NotEmpty().WithMessage("Password is required.")
                    .MinimumLength(6).WithMessage("Password must be at least 6 characters long.");

                RuleFor(x => x.FirstName)
                    .MaximumLength(50).WithMessage("First name cannot exceed 50 characters.")
                    .When(x => !string.IsNullOrEmpty(x.FirstName));

                RuleFor(x => x.LastName)
                    .MaximumLength(50).WithMessage("Last name cannot exceed 50 characters.")
                    .When(x => !string.IsNullOrEmpty(x.LastName));

                RuleFor(x => x.PhoneNumber)
                    .MaximumLength(20).WithMessage("Phone number cannot exceed 20 characters.")
                    .When(x => !string.IsNullOrEmpty(x.PhoneNumber));
            }
        }
    }

    public sealed class RegisterEndpoint(IIdentityService identityService)
        : Endpoint<RegisterRequestDto, RegisterResponse>
    {
        public override void Configure()
        {
            Post("/auth/register");
            AllowAnonymous();
            Description(b => b
                .WithDescription("Registers a new user with email and password.")
                .WithTags("Auth"));
        }

        public override async Task HandleAsync(RegisterRequestDto req, CancellationToken ct)
        {
            var request = new RegisterRequest
            {
                Email = req.Email,
                Password = req.Password,
                FirstName = req.FirstName,
                LastName = req.LastName,
                PhoneNumber = req.PhoneNumber
            };

            var result = await identityService.RegisterWithEmailAsync(request);

            await Send.OkAsync(result, ct);
        }
    }
}
