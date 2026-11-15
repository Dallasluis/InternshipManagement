using InternshipManagement.Application.DTOs.Application;
using InternshipManagement.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InternshipManagement.Api.Controllers
{
    /// <summary>Changing an interview that is already scheduled: move it to a new time, or cancel it.</summary>
    [ApiController]
    [Route("api/Applications")]
    [Authorize(Roles = "Company")]
    public class ApplicationInterviewsController : ControllerBase
    {
        private readonly IApplicationService _applications;

        public ApplicationInterviewsController(IApplicationService applications)
        {
            _applications = applications;
        }

        [HttpPost("{id:int}/reschedule-interview")]
        public async Task<IActionResult> Reschedule(int id, [FromBody] ScheduleInterviewRequest request)
        {
            try
            {
                var done = await _applications.RescheduleInterviewAsync(GetUserId(), id, request);
                return done ? Ok(new { Success = true }) : NotFound();
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPost("{id:int}/cancel-interview")]
        public async Task<IActionResult> Cancel(int id, [FromBody] CancelInterviewRequest? request)
        {
            try
            {
                var done = await _applications.CancelInterviewAsync(GetUserId(), id, request?.Reason);
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
