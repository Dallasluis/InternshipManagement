using InternshipManagement.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InternshipManagement.Api.Controllers
{
    /// <summary>Lets administrators read the audit log. There is no way to change or delete entries.</summary>
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminAuditController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public AdminAuditController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet("api/admin/audit-logs")]
        public async Task<IActionResult> Get(
            string? actionName, string? role, string? user, DateTime? from, DateTime? to,
            int page = 1, int pageSize = 50, CancellationToken ct = default)
        {
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 10, 200);

            var query = _db.AuditLogs.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(actionName)) query = query.Where(a => a.Action.Contains(actionName.Trim()));
            if (!string.IsNullOrWhiteSpace(role)) query = query.Where(a => a.Role == role.Trim());
            if (!string.IsNullOrWhiteSpace(user)) query = query.Where(a => a.UserEmail != null && a.UserEmail.Contains(user.Trim()));
            if (from.HasValue) query = query.Where(a => a.CreatedAt >= from.Value);
            if (to.HasValue) query = query.Where(a => a.CreatedAt < to.Value.Date.AddDays(1));

            var total = await query.CountAsync(ct);
            var items = await query
                .OrderByDescending(a => a.CreatedAt)
                .ThenByDescending(a => a.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return Ok(new { items, total, page, pageSize });
        }
    }
}
