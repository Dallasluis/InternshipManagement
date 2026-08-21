using FluentValidation;
using InternshipManagement.Application.DTOs.Application;
using InternshipManagement.Domain.Enums;

namespace InternshipManagement.Application.Validators
{
    public class ApplyRequestValidator : AbstractValidator<ApplyRequest>
    {
        public ApplyRequestValidator()
        {
            RuleFor(x => x.InternshipId)
                .GreaterThan(0).WithMessage("Invalid internship ID");

            When(x => !string.IsNullOrEmpty(x.CoverLetter), () =>
            {
                RuleFor(x => x.CoverLetter)
                    .MinimumLength(50).WithMessage("Cover letter must be at least 50 characters")
                    .MaximumLength(2000).WithMessage("Cover letter cannot exceed 2000 characters");
            });

            When(x => x.AdditionalDocumentUrls != null, () =>
            {
                RuleFor(x => x.AdditionalDocumentUrls)
                    .Must(x => x.Count <= 5).WithMessage("Maximum 5 additional documents allowed");
            });
        }
    }

    public class UpdateApplicationStatusValidator : AbstractValidator<UpdateApplicationStatusRequest>
    {
        public UpdateApplicationStatusValidator()
        {
            RuleFor(x => x.Status)
                .NotEmpty().WithMessage("Status is required")
                .Must(BeValidStatus).WithMessage("Invalid application status");
        }

        private bool BeValidStatus(string status)
        {
            return Enum.TryParse<ApplicationStatus>(status, true, out _);
        }
    }
}