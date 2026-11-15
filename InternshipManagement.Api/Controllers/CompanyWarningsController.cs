using System.ComponentModel.DataAnnotations;
using InternshipManagement.Application.Interfaces;
using InternshipManagement.Domain.Entities;
using InternshipManagement.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InternshipManagement.Api.Controllers
{
    /// <summary>A formal warning to a company that broke platform policy, short of suspending it.</summary>
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class CompanyWarningsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly INotificationService _notifications;

        public CompanyWarningsController(ApplicationDbContext db, INotificationService notifications)
        {
            _db = db;
            _notifications = notifications;
        }

        [HttpPost("api/admin/companies/{companyId:int}/warn")]
        public async Task<IActionResult> Warn(int companyId, [FromBody] WarnRequest request, CancellationToken ct)
        {
            var company = await _db.CompanyProfiles.AsNoTracking()
                .Where(c => c.Id == companyId)
                .Select(c => new { c.Id, c.UserId, c.CompanyName })
                .FirstOrDefaultAsync(ct);
            if (company == null) return NotFound(new { Error = "Company not found." });

            var message = request.Message.Trim();
            _db.CompanyWarnings.Add(new CompanyWarning
            {
                CompanyProfileId = company.Id,
                AdminUserId = int.Parse(User.FindFirst("userId")?.Value ?? "0"),
                Message = message
            });
            await _db.SaveChangesAsync(ct);

            await _notifications.NotifyAsync(company.UserId, NotificationTypes.Moderation,
                "Warning from the platform administrators",
                message, "/Company/Dashboard", sendEmail: true, cancellationToken: ct);

            return Ok(new { Success = true });
        }

        [HttpGet("api/admin/companies/{companyId:int}/warnings")]
        public async Task<IActionResult> List(int companyId, CancellationToken ct) =>
            Ok(await _db.CompanyWarnings.AsNoTracking()
                .Where(w => w.CompanyProfileId == companyId)
                .OrderByDescending(w => w.CreatedAt)
                .Select(w => new { w.Id, w.Message, w.CreatedAt })
                .ToListAsync(ct));

        public sealed class WarnRequest
        {
            [Required, StringLength(2000, MinimumLength = 10)]
            public string Message { get; set; } = string.Empty;
        }
    }
}
