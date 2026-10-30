using InternshipManagement.Application.DTOs.Reports;
using InternshipManagement.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InternshipManagement.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Student")]
    public class ReportsController : ControllerBase
    {
        private readonly IReportService _reportService;

        public ReportsController(IReportService reportService)
        {
            _reportService = reportService;
        }

        [HttpPost]
        public async Task<IActionResult> SubmitInternshipReport([FromBody] SubmitInternshipReportRequest request)
        {
            var reporterId = int.Parse(User.FindFirst("userId")?.Value ?? "0");
            var report = await _reportService.SubmitInternshipReportAsync(reporterId, request);

            if (report == null)
                return NotFound(new { Error = "Published internship not found." });

            return StatusCode(StatusCodes.Status201Created, report);
        }
    }
}
