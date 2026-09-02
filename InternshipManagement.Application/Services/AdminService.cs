using Microsoft.EntityFrameworkCore;
using InternshipManagement.Application.DTOs.Admin;
using InternshipManagement.Application.Interfaces;
using InternshipManagement.Domain.Enums;

namespace InternshipManagement.Application.Services
{
    public class AdminService : IAdminService
    {
        private readonly IApplicationDbContext _context;
        private readonly IIdentityService _identityService;

        public AdminService(IApplicationDbContext context, IIdentityService identityService)
        {
            _context = context;
            _identityService = identityService;
        }

        public async Task<AdminStatsResponse> GetStatsAsync()
        {
            var users = await _identityService.GetAllUsersAsync();

            var totalInternships = await _context.Internships.CountAsync(i => !i.IsDeleted);
            var activeInternships = await _context.Internships.CountAsync(i => !i.IsDeleted && i.Status == InternshipStatus.Published);
            var totalApplications = await _context.InternshipApplications.CountAsync(a => !a.IsDeleted);
            var pendingReviews = await _context.Internships.CountAsync(i => !i.IsDeleted && i.ModerationStatus == ModerationStatus.Pending);
            var pendingVerifications = await _context.CompanyProfiles.CountAsync(c => !c.IsDeleted &&
                (c.VerificationStatus == CompanyVerificationStatus.Pending || c.VerificationStatus == CompanyVerificationStatus.UnderReview));
            var pendingReports = await _context.Reports.CountAsync(r => !r.IsDeleted && r.Status == ReportStatus.Pending);

            var monthlyTrend = await BuildMonthlyTrendAsync();

            return new AdminStatsResponse
            {
                TotalUsers = users.Count,
                TotalStudents = users.Count(u => u.UserType == "Student"),
                TotalCompanies = users.Count(u => u.UserType == "Company"),
                TotalInternships = totalInternships,
                ActiveInternships = activeInternships,
                TotalApplications = totalApplications,
                PendingReviews = pendingReviews,
                PendingVerifications = pendingVerifications,
                PendingReports = pendingReports,
                MonthlyTrend = monthlyTrend
            };
        }

        private async Task<List<MonthlyStatDto>> BuildMonthlyTrendAsync()
        {
            var since = DateTime.UtcNow.AddMonths(-5).Date;
            since = new DateTime(since.Year, since.Month, 1);

            var internships = await _context.Internships
                .Where(i => !i.IsDeleted && i.CreatedAt >= since)
                .Select(i => i.CreatedAt)
                .ToListAsync();

            var applications = await _context.InternshipApplications
                .Where(a => !a.IsDeleted && a.CreatedAt >= since)
                .Select(a => a.CreatedAt)
                .ToListAsync();

            var users = await _identityService.GetAllUsersAsync();
            var userDates = users.Where(u => u.CreatedAt >= since).Select(u => u.CreatedAt).ToList();

            var result = new List<MonthlyStatDto>();
            for (var i = 0; i < 6; i++)
            {
                var month = since.AddMonths(i);
                result.Add(new MonthlyStatDto
                {
                    Month = month.ToString("MMM yyyy"),
                    Internships = internships.Count(d => d.Year == month.Year && d.Month == month.Month),
                    Applications = applications.Count(d => d.Year == month.Year && d.Month == month.Month),
                    Users = userDates.Count(d => d.Year == month.Year && d.Month == month.Month)
                });
            }

            return result;
        }

        public async Task<List<CompanyListResponse>> GetAllCompaniesAsync()
        {
            var users = await _identityService.GetAllUsersAsync();
            var userLookup = users.ToDictionary(u => u.Id, u => u);

            var companies = await _context.CompanyProfiles
                .Where(c => !c.IsDeleted)
                .Include(c => c.Internships)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            return companies.Select(c =>
            {
                userLookup.TryGetValue(c.UserId, out var user);
                return new CompanyListResponse
                {
                    Id = c.Id,
                    UserId = c.UserId,
                    CompanyName = c.CompanyName,
                    Industry = c.Industry,
                    VerificationStatus = c.VerificationStatus.ToString(),
                    Email = user?.Email,
                    PhoneNumber = c.PhoneNumber,
                    CreatedAt = c.CreatedAt,
                    InternshipCount = c.Internships?.Count(i => !i.IsDeleted) ?? 0
                };
            }).ToList();
        }

        public async Task<List<CompanySummaryResponse>> GetRecentCompaniesAsync(int count)
        {
            var companies = await _context.CompanyProfiles
                .Where(c => !c.IsDeleted)
                .OrderByDescending(c => c.CreatedAt)
                .Take(count)
                .ToListAsync();

            return companies.Select(c => new CompanySummaryResponse
            {
                Id = c.Id,
                CompanyName = c.CompanyName,
                Industry = c.Industry,
                VerificationStatus = c.VerificationStatus.ToString(),
                CreatedAt = c.CreatedAt
            }).ToList();
        }

        public async Task<List<InternshipListResponse>> GetAllInternshipsAsync()
        {
            var internships = await _context.Internships
                .Where(i => !i.IsDeleted)
                .Include(i => i.CompanyProfile)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();

            return internships.Select(i => new InternshipListResponse
            {
                Id = i.Id,
                Title = i.Title,
                CompanyName = i.CompanyProfile?.CompanyName ?? string.Empty,
                Industry = i.Industry,
                Status = i.Status.ToString(),
                ModerationStatus = i.ModerationStatus.ToString(),
                CreatedAt = i.CreatedAt,
                ApplicationsCount = i.ApplicationsCount
            }).ToList();
        }

        public async Task<List<ReportListResponse>> GetAllReportsAsync()
        {
            var users = await _identityService.GetAllUsersAsync();
            var userLookup = users.ToDictionary(u => u.Id, u => u);

            var reports = await _context.Reports
                .Where(r => !r.IsDeleted)
                .Include(r => r.Internship)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return reports.Select(r =>
            {
                userLookup.TryGetValue(r.ReporterId, out var reporter);
                return new ReportListResponse
                {
                    Id = r.Id,
                    Type = r.Type.ToString(),
                    Description = r.Description,
                    Status = r.Status.ToString(),
                    InternshipTitle = r.Internship?.Title ?? string.Empty,
                    ReporterName = reporter != null ? $"{reporter.FirstName} {reporter.LastName}".Trim() : "Unknown",
                    CreatedAt = r.CreatedAt,
                    AdminResponse = r.AdminResponse
                };
            }).ToList();
        }

        public async Task<List<ReportSummaryResponse>> GetRecentReportsAsync(int count)
        {
            var reports = await _context.Reports
                .Where(r => !r.IsDeleted)
                .Include(r => r.Internship)
                .OrderByDescending(r => r.CreatedAt)
                .Take(count)
                .ToListAsync();

            return reports.Select(r => new ReportSummaryResponse
            {
                Id = r.Id,
                Type = r.Type.ToString(),
                InternshipTitle = r.Internship?.Title ?? string.Empty,
                Status = r.Status.ToString(),
                CreatedAt = r.CreatedAt
            }).ToList();
        }

        public async Task<bool> ResolveReportAsync(int reportId, string response, bool resolved)
        {
            var report = await _context.Reports.FirstOrDefaultAsync(r => r.Id == reportId && !r.IsDeleted);
            if (report == null) return false;

            report.AdminResponse = response;
            report.Status = resolved ? ReportStatus.Resolved : ReportStatus.Dismissed;
            report.ResolvedAt = DateTime.UtcNow;
            report.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<UserListResponse>> GetAllUsersAsync()
        {
            var users = await _identityService.GetAllUsersAsync();
            return users.Select(u => new UserListResponse
            {
                Id = u.Id,
                Email = u.Email,
                FirstName = u.FirstName,
                LastName = u.LastName,
                UserType = u.UserType,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt,
                LastLoginAt = u.LastLoginAt
            }).ToList();
        }

        public async Task<bool> SuspendUserAsync(int userId, bool suspend)
        {
            return await _identityService.SetUserActiveStatusAsync(userId.ToString(), !suspend);
        }
    }
}
