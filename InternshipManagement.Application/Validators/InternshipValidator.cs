using FluentValidation;
using InternshipManagement.Application.DTOs.Internship;
using InternshipManagement.Domain.Enums;

namespace InternshipManagement.Application.Validators
{
    public class CreateInternshipValidator : AbstractValidator<CreateInternshipRequest>
    {
        public CreateInternshipValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Title is required")
                .MaximumLength(200).WithMessage("Title cannot exceed 200 characters");

            RuleFor(x => x.Description)
                .NotEmpty().WithMessage("Description is required")
                .MinimumLength(50).WithMessage("Description must be at least 50 characters");

            RuleFor(x => x.Industry)
                .NotEmpty().WithMessage("Industry is required")
                .MaximumLength(100).WithMessage("Industry cannot exceed 100 characters");

            RuleFor(x => x.InternshipType)
                .NotEmpty().WithMessage("Internship type is required")
                .Must(BeValidInternshipType).WithMessage("Invalid internship type");

            RuleFor(x => x.Duration)
                .NotEmpty().WithMessage("Duration is required")
                .Must(BeValidDuration).WithMessage("Invalid duration");

            RuleFor(x => x.StartDate)
                .GreaterThan(DateTime.UtcNow).WithMessage("Start date must be in the future");

            RuleFor(x => x.EndDate)
                .GreaterThan(x => x.StartDate).WithMessage("End date must be after start date")
                .When(x => x.EndDate.HasValue);

            RuleFor(x => x.ApplicationDeadline)
                .GreaterThan(DateTime.UtcNow).WithMessage("Application deadline must be in the future")
                .LessThan(x => x.StartDate).WithMessage("Application deadline must be before start date");

            RuleFor(x => x.NumberOfPositions)
                .GreaterThan(0).WithMessage("Number of positions must be at least 1")
                .LessThanOrEqualTo(100).WithMessage("Number of positions cannot exceed 100");

            When(x => !string.IsNullOrEmpty(x.Compensation) && x.Compensation == "Stipend", () =>
            {
                RuleFor(x => x.StipendAmount)
                    .NotNull().WithMessage("Stipend amount is required when compensation is 'Stipend'")
                    .GreaterThan(0).WithMessage("Stipend amount must be greater than 0");
            });

            When(x => !string.IsNullOrEmpty(x.Location) && !x.IsRemote, () =>
            {
                RuleFor(x => x.Location)
                    .NotEmpty().WithMessage("Location is required for non-remote internships");
            });
        }

        private bool BeValidInternshipType(string type)
        {
            return Enum.TryParse<InternshipType>(type, true, out _);
        }

        private bool BeValidDuration(string duration)
        {
            return Enum.TryParse<InternshipDuration>(duration, true, out _);
        }
    }

    // ✅ CORRECT - Separate validator with its own rules
    public class UpdateInternshipValidator : AbstractValidator<UpdateInternshipRequest>
    {
        public UpdateInternshipValidator()
        {
            // Id validation
            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage("Invalid internship ID");

            // Copy all the same rules as CreateInternshipValidator
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Title is required")
                .MaximumLength(200).WithMessage("Title cannot exceed 200 characters");

            RuleFor(x => x.Description)
                .NotEmpty().WithMessage("Description is required")
                .MinimumLength(50).WithMessage("Description must be at least 50 characters");

            RuleFor(x => x.Industry)
                .NotEmpty().WithMessage("Industry is required")
                .MaximumLength(100).WithMessage("Industry cannot exceed 100 characters");

            RuleFor(x => x.InternshipType)
                .NotEmpty().WithMessage("Internship type is required")
                .Must(BeValidInternshipType).WithMessage("Invalid internship type");

            RuleFor(x => x.Duration)
                .NotEmpty().WithMessage("Duration is required")
                .Must(BeValidDuration).WithMessage("Invalid duration");

            RuleFor(x => x.StartDate)
                .GreaterThan(DateTime.UtcNow).WithMessage("Start date must be in the future");

            RuleFor(x => x.EndDate)
                .GreaterThan(x => x.StartDate).WithMessage("End date must be after start date")
                .When(x => x.EndDate.HasValue);

            RuleFor(x => x.ApplicationDeadline)
                .GreaterThan(DateTime.UtcNow).WithMessage("Application deadline must be in the future")
                .LessThan(x => x.StartDate).WithMessage("Application deadline must be before start date");

            RuleFor(x => x.NumberOfPositions)
                .GreaterThan(0).WithMessage("Number of positions must be at least 1")
                .LessThanOrEqualTo(100).WithMessage("Number of positions cannot exceed 100");

            When(x => !string.IsNullOrEmpty(x.Compensation) && x.Compensation == "Stipend", () =>
            {
                RuleFor(x => x.StipendAmount)
                    .NotNull().WithMessage("Stipend amount is required when compensation is 'Stipend'")
                    .GreaterThan(0).WithMessage("Stipend amount must be greater than 0");
            });

            When(x => !string.IsNullOrEmpty(x.Location) && !x.IsRemote, () =>
            {
                RuleFor(x => x.Location)
                    .NotEmpty().WithMessage("Location is required for non-remote internships");
            });
        }

        private bool BeValidInternshipType(string type)
        {
            return Enum.TryParse<InternshipType>(type, true, out _);
        }

        private bool BeValidDuration(string duration)
        {
            return Enum.TryParse<InternshipDuration>(duration, true, out _);
        }
    }
}