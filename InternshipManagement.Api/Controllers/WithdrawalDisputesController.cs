using InternshipManagement.Application.DTOs.Lifecycle;
using InternshipManagement.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using InternshipManagement.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InternshipManagement.Api.Controllers
{
    /// <summary>
    /// When a company turns down a withdrawal, the intern can dispute it once. An administrator then
    /// either ends the placement as the intern asked or keeps the company's decision.
    /// </summary>
    [ApiController]
    [Authorize]
    public class WithdrawalDisputesController : ControllerBase
    {
        private readonly IInternshipLifecycleService _lifecycle;
        private readonly INotificationService _notifications;
        private readonly ApplicationDbContext _db;

        public WithdrawalDisputesController(
            IInternshipLifecycleService lifecycle, INotificationService notifications, ApplicationDbContext db)
        {
            _lifecycle = lifecycle;
            _notifications = notifications;
            _db = db;
        }

        [HttpPost("api/lifecycle/withdrawals/{id:int}/dispute")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Dispute(int id, [FromBody] DisputeWithdrawalRequest request)
        {
            try
            {
                var result = await _lifecycle.DisputeWithdrawalAsync(GetUserId(), id, request.Reason);
                await NotifyAdminsAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        // Tell every active administrator that a dispute is waiting. A failure here must never undo the dispute.
        private async Task NotifyAdminsAsync()
        {
            try
            {
                var adminIds = await _db.Users.AsNoTracking()
                    .Where(u => u.UserType == "Admin" && u.IsActive)
                    .Select(u => u.Id)
                    .Take(20)
                    .ToListAsync();

                foreach (var adminId in adminIds)
                    await _notifications.NotifyAsync(adminId, NotificationTypes.Withdrawal,
                        "A withdrawal dispute needs review",
                        "A student disputed a company's decision on their withdrawal. Please review it.",
                        "/AdminWithdrawals", sendEmail: true);
            }
            catch
            {
                // Notifications are best effort.
            }
        }

        [HttpGet("api/admin/withdrawals/disputed")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Disputed() => Ok(await _lifecycle.GetDisputedWithdrawalsAsync());

        [HttpPost("api/admin/withdrawals/{id:int}/resolve")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Resolve(int id, [FromBody] ResolveWithdrawalDisputeRequest request)
        {
            try
            {
                var done = await _lifecycle.ResolveWithdrawalDisputeAsync(GetUserId(), id, request);
                return done ? Ok(new { Success = true }) : NotFound();
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        private int GetUserId() => int.Parse(User.FindFirst("userId")?.Value ?? "0");
    }
}
