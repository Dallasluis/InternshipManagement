using InternshipManagement.Application.DTOs.Reports;
using InternshipManagement.Application.Interfaces;
using InternshipManagement.Domain.Entities;
using InternshipManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace InternshipManagement.Application.Services
{
    public class ReportService : IReportService
    {
        private readonly IApplicationDbContext _context;

        public ReportService(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<InternshipReportResponse?> SubmitInternshipReportAsync(
            int reporterId,
            SubmitInternshipReportRequest request)
        {
            var internship = await _context.Internships.FirstOrDefaultAsync(i =>
                i.Id == request.InternshipId &&
                !i.IsDeleted &&
                i.Status == InternshipStatus.Published &&
                i.ModerationStatus != ModerationStatus.Flagged &&
                i.ModerationStatus != ModerationStatus.Removed &&
                i.ModerationStatus != ModerationStatus.Rejected &&
                (i.CompanyProfile.VerificationStatus == CompanyVerificationStatus.Verified
                    || (i.CompanyProfile.VerificationStatus == CompanyVerificationStatus.ReverificationRequired
                        && i.CompanyProfile.ReverificationRequiredAt >= DateTime.UtcNow.AddDays(-7))));

            if (internship == null)
                return null;

            if (!Enum.TryParse<ReportType>(request.Type, true, out var type) || !Enum.IsDefined(type))
                throw new ArgumentException("The report type is invalid.", nameof(request));

            var report = new Report
            {
                ReporterId = reporterId,
                InternshipId = internship.Id,
                Type = type,
                Description = request.Description.Trim(),
                Status = ReportStatus.Pending
            };

            _context.Reports.Add(report);
            await _context.SaveChangesAsync();

            return new InternshipReportResponse
            {
                Id = report.Id,
                InternshipId = report.InternshipId,
                Type = report.Type.ToString(),
                Status = report.Status.ToString(),
                CreatedAt = report.CreatedAt
            };
        }
    }
}
