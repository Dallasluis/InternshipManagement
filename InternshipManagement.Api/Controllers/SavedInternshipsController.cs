using InternshipManagement.Domain.Entities;
using InternshipManagement.Domain.Enums;
using InternshipManagement.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InternshipManagement.Api.Controllers
{
    /// <summary>A student's bookmarked internships.</summary>
    [ApiController]
    [Authorize(Roles = "Student")]
    [Route("api/students/saved-internships")]
    public class SavedInternshipsController : ControllerBase
    {
        private const int MaxSaved = 200;
        private readonly ApplicationDbContext _db;

        public SavedInternshipsController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> List(CancellationToken ct)
        {
            var userId = GetUserId();

            // joined to Internships, so postings that were deleted simply drop out of the list
            var items = await (from s in _db.SavedInternships.AsNoTracking()
                               join i in _db.Internships.AsNoTracking() on s.InternshipId equals i.Id
                               where s.UserId == userId
                               orderby s.CreatedAt descending
                               select new
                               {
                                   InternshipId = i.Id,
                                   i.Title,
                                   CompanyName = i.CompanyProfile.CompanyName,
                                   i.Location,
                                   i.ApplicationDeadline,
                                   i.Status,
                                   SavedAt = s.CreatedAt
                               }).ToListAsync(ct);

            // the status is turned into text here, after the query, so it reads "Published" rather than a number
            return Ok(items.Select(x => new { x.InternshipId, x.Title, x.CompanyName, x.Location, x.ApplicationDeadline, Status = x.Status.ToString(), x.SavedAt }));
        }

        [HttpGet("ids")]
        public async Task<IActionResult> Ids(CancellationToken ct)
        {
            var userId = GetUserId();
            return Ok(await _db.SavedInternships.AsNoTracking().Where(s => s.UserId == userId).Select(s => s.InternshipId).ToListAsync(ct));
        }

        [HttpPost("{internshipId:int}")]
        public async Task<IActionResult> Save(int internshipId, CancellationToken ct)
        {
            var userId = GetUserId();

            if (await _db.SavedInternships.AnyAsync(s => s.UserId == userId && s.InternshipId == internshipId, ct))
                return Ok(new { Saved = true });

            var exists = await _db.Internships.AnyAsync(i => i.Id == internshipId && i.Status == InternshipStatus.Published, ct);
            if (!exists)
                return NotFound(new { Error = "That internship is not available." });

            if (await _db.SavedInternships.CountAsync(s => s.UserId == userId, ct) >= MaxSaved)
                return BadRequest(new { Error = $"You can save up to {MaxSaved} internships. Remove some first." });

            _db.SavedInternships.Add(new SavedInternship { UserId = userId, InternshipId = internshipId });
            try
            {
                await _db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                // two clicks at once: the unique index already has it, which is what was wanted
            }

            return Ok(new { Saved = true });
        }

        [HttpDelete("{internshipId:int}")]
        public async Task<IActionResult> Remove(int internshipId, CancellationToken ct)
        {
            var userId = GetUserId();
            await _db.SavedInternships.Where(s => s.UserId == userId && s.InternshipId == internshipId).ExecuteDeleteAsync(ct);
            return Ok(new { Saved = false });
        }

        private int GetUserId() => int.Parse(User.FindFirst("userId")?.Value ?? "0");
    }
}
