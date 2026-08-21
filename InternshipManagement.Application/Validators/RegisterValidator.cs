using FluentValidation;
using InternshipManagement.Application.DTOs.Auth;

namespace InternshipManagement.Application.Validators
{
    public class RegisterValidator : AbstractValidator<RegisterRequest>
    {
        public RegisterValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("Invalid email format");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required")
                .MinimumLength(8).WithMessage("Password must be at least 8 characters")
                .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter")
                .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter")
                .Matches("[0-9]").WithMessage("Password must contain at least one number");

            RuleFor(x => x.ConfirmPassword)
                .Equal(x => x.Password).WithMessage("Passwords do not match");

            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("First name is required")
                .MaximumLength(50).WithMessage("First name cannot exceed 50 characters");

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Last name is required")
                .MaximumLength(50).WithMessage("Last name cannot exceed 50 characters");

            RuleFor(x => x.UserType)
                .Must(x => string.IsNullOrEmpty(x) || x == "Student" || x == "Company")
                .WithMessage("User type must be either 'Student' or 'Company'");

            When(x => x.UserType == "Company", () =>
            {
                RuleFor(x => x.CompanyName)
                    .NotEmpty().WithMessage("Company name is required")
                    .MaximumLength(200).WithMessage("Company name cannot exceed 200 characters");

                RuleFor(x => x.Industry)
                    .NotEmpty().WithMessage("Industry is required")
                    .MaximumLength(100).WithMessage("Industry cannot exceed 100 characters");
            });

            When(x => !string.IsNullOrEmpty(x.PhoneNumber), () =>
            {
                RuleFor(x => x.PhoneNumber)
                    .Matches(@"^\+?[1-9]\d{1,14}$")
                    .WithMessage("Invalid phone number format");
            });
        }
    }
}