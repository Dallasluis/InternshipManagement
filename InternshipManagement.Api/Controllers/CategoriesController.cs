using System.ComponentModel.DataAnnotations;
using InternshipManagement.Domain.Entities;
using InternshipManagement.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InternshipManagement.Api.Controllers
{
    /// <summary>The list of fields and industries internships are filed under. Everyone reads it; administrators manage it.</summary>
    [ApiController]
    [Authorize]
    public class CategoriesController : ControllerBase
    {
        // a neutral starting list that suits any course, not only technology
        private static readonly string[] Defaults =
        {
            "Accounting & Finance", "Agriculture & Environment", "Architecture & Construction", "Arts & Design",
            "Business & Management", "Education & Training", "Engineering", "Healthcare & Medicine",
            "Hospitality & Tourism", "Human Resources", "Information Technology", "Law & Public Policy",
            "Marketing & Communications", "Media & Journalism", "Retail & Sales", "Science & Research",
            "Social Work & Community", "Transport & Logistics"
        };

        private readonly ApplicationDbContext _db;

        public CategoriesController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet("api/categories")]
        public async Task<IActionResult> Active(CancellationToken ct)
        {
            await EnsureDefaultsAsync(ct);
            return Ok(await _db.InternshipCategories.AsNoTracking()
                .Where(c => c.IsActive)
                .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
                .Select(c => c.Name)
                .ToListAsync(ct));
        }

        [HttpGet("api/admin/categories")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> All(CancellationToken ct)
        {
            await EnsureDefaultsAsync(ct);
            return Ok(await _db.InternshipCategories.AsNoTracking()
                .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
                .ToListAsync(ct));
        }

        [HttpPost("api/admin/categories")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] CategoryRequest request, CancellationToken ct)
        {
            var name = request.Name.Trim();
            if (await _db.InternshipCategories.AnyAsync(c => c.Name == name, ct))
                return BadRequest(new { Error = "That category already exists." });

            var next = (await _db.InternshipCategories.MaxAsync(c => (int?)c.SortOrder, ct) ?? 0) + 1;
            _db.InternshipCategories.Add(new InternshipCategory { Name = name, SortOrder = next });
            await _db.SaveChangesAsync(ct);
            return Ok(new { Success = true });
        }

        [HttpPut("api/admin/categories/{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] CategoryRequest request, CancellationToken ct)
        {
            var category = await _db.InternshipCategories.FirstOrDefaultAsync(c => c.Id == id, ct);
            if (category == null) return NotFound();

            var name = request.Name.Trim();
            if (await _db.InternshipCategories.AnyAsync(c => c.Name == name && c.Id != id, ct))
                return BadRequest(new { Error = "Another category already has that name." });

            category.Name = name;
            category.IsActive = request.IsActive;
            await _db.SaveChangesAsync(ct);
            return Ok(new { Success = true });
        }

        [HttpDelete("api/admin/categories/{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            // internships keep their own text, so removing a category never changes existing postings
            var removed = await _db.InternshipCategories.Where(c => c.Id == id).ExecuteDeleteAsync(ct);
            return removed == 0 ? NotFound() : Ok(new { Success = true });
        }

        private async Task EnsureDefaultsAsync(CancellationToken ct)
        {
            if (await _db.InternshipCategories.AnyAsync(ct)) return;

            try
            {
                var order = 0;
                _db.InternshipCategories.AddRange(Defaults.Select(n => new InternshipCategory { Name = n, SortOrder = ++order }));
                await _db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                _db.ChangeTracker.Clear(); // another request seeded them first
            }
        }

        public sealed class CategoryRequest
        {
            [Required, StringLength(100, MinimumLength = 2)]
            public string Name { get; set; } = string.Empty;
            public bool IsActive { get; set; } = true;
        }
    }
}
