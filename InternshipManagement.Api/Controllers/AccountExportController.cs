using InternshipManagement.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InternshipManagement.Api.Controllers
{
    /// <summary>Lets any signed-in user download a copy of the personal data the platform holds about them.</summary>
    [ApiController]
    [Authorize]
    [Route("api/account/export")]
    public class AccountExportController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public AccountExportController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> Export(CancellationToken ct)
        {
            var userId = int.Parse(User.FindFirst("userId")?.Value ?? "0");
            if (userId == 0) return Unauthorized();

            var account = await _db.Users.AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => new { u.Email, u.UserName, u.PhoneNumber, u.EmailConfirmed })
                .FirstOrDefaultAsync(ct);

            var notifications = await _db.Notifications.AsNoTracking()
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt).Take(200)
                .Select(n => new { n.Type, n.Title, n.Message, n.IsRead, n.CreatedAt })
                .ToListAsync(ct);

            var securityEvents = await _db.AccountSecurityEvents.AsNoTracking()
                .Where(e => e.UserId == userId)
                .OrderByDescending(e => e.CreatedAt).Take(100)
                .Select(e => new { e.EventType, e.IpAddress, e.CreatedAt })
                .ToListAsync(ct);

            // file names and types only: the files themselves are downloaded from the profile page
            var documents = await _db.PrivateDocuments.AsNoTracking()
                .Where(d => d.OwnerUserId == userId)
                .Select(d => new { d.Category, d.OriginalFileName, d.ContentType, d.Length, d.CreatedAt })
                .ToListAsync(ct);

            object? student = null, company = null;

            if (User.IsInRole("Student"))
            {
                var profile = await _db.StudentProfiles.AsNoTracking().FirstOrDefaultAsync(s => s.UserId == userId, ct);
                if (profile != null)
                {
                    var id = profile.Id;
                    student = new
                    {
                        Profile = new
                        {
                            profile.FirstName, profile.LastName, profile.Bio, profile.Location, profile.PhoneNumber,
                            profile.LinkedInUrl, profile.PortfolioUrl, profile.University, profile.Programme,
                            profile.YearOfStudy, profile.ExpectedGraduationYear
                        },
                        Education = await _db.Educations.AsNoTracking().Where(e => e.StudentProfileId == id)
                            .Select(e => new { e.Institution, e.Degree, e.FieldOfStudy, e.StartDate, e.EndDate, e.IsCurrent, e.Grade }).ToListAsync(ct),
                        WorkExperience = await _db.WorkExperiences.AsNoTracking().Where(w => w.StudentProfileId == id)
                            .Select(w => new { w.Company, w.Position, w.Description, w.StartDate, w.EndDate, w.IsCurrent }).ToListAsync(ct),
                        Skills = await _db.Skills.AsNoTracking().Where(s => s.StudentProfileId == id)
                            .Select(s => new { s.Name, s.ProficiencyLevel }).ToListAsync(ct),
                        Certifications = await _db.StudentCertifications.AsNoTracking().Where(c => c.StudentProfileId == id)
                            .Select(c => new { c.Name, c.IssuingOrganization, c.IssueDate, c.ExpirationDate }).ToListAsync(ct),
                        Projects = await _db.StudentProjects.AsNoTracking().Where(p => p.StudentProfileId == id)
                            .Select(p => new { p.Title, p.Description, p.Role, p.Technologies, p.ProjectUrl, p.RepositoryUrl }).ToListAsync(ct),
                        Applications = (await _db.InternshipApplications.AsNoTracking().Where(a => a.StudentProfileId == id)
                            .OrderByDescending(a => a.CreatedAt)
                            .Select(a => new
                            {
                                a.Id,
                                Internship = a.Internship.Title,
                                Company = a.Internship.CompanyProfile.CompanyName,
                                a.Status,
                                Applied = a.CreatedAt,
                                a.CoverLetter,
                                a.InterviewDateTime,
                                a.OfferDetails
                            }).ToListAsync(ct)).Select(a => new
                            {
                                a.Id, a.Internship, a.Company, Status = a.Status.ToString(),
                                a.Applied, a.CoverLetter, a.InterviewDateTime, a.OfferDetails
                            }).ToList(),
                        SavedInternships = await _db.SavedInternships.AsNoTracking().Where(s => s.UserId == userId)
                            .Select(s => new { s.InternshipId, Saved = s.CreatedAt }).ToListAsync(ct)
                    };
                }
            }
            else if (User.IsInRole("Company"))
            {
                var profile = await _db.CompanyProfiles.AsNoTracking().FirstOrDefaultAsync(c => c.UserId == userId, ct);
                if (profile != null)
                {
                    var id = profile.Id;
                    company = new
                    {
                        Profile = new
                        {
                            profile.CompanyName, profile.LegalName, profile.RegistrationNumber, profile.Description,
                            profile.Industry, profile.Website, profile.Address, profile.City, profile.Country, profile.PhoneNumber,
                            VerificationStatus = profile.VerificationStatus.ToString(), profile.VerifiedAt
                        },
                        Internships = (await _db.Internships.AsNoTracking().Where(i => i.CompanyProfileId == id)
                            .OrderByDescending(i => i.CreatedAt)
                            .Select(i => new { i.Id, i.Title, i.Status, i.ApplicationDeadline, i.CreatedAt }).ToListAsync(ct))
                            .Select(i => new { i.Id, i.Title, Status = i.Status.ToString(), i.ApplicationDeadline, i.CreatedAt }).ToList(),
                        WarningsReceived = await _db.CompanyWarnings.AsNoTracking().Where(w => w.CompanyProfileId == id)
                            .Select(w => new { w.Message, w.CreatedAt }).ToListAsync(ct)
                    };
                }
            }

            return Ok(new
            {
                ExportedAtUtc = DateTime.UtcNow,
                Account = account,
                Student = student,
                Company = company,
                Documents = documents,
                Notifications = notifications,
                SecurityEvents = securityEvents
            });
        }
    }
}
