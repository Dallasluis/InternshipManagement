using InternshipManagement.Application.DTOs.Admin;
using InternshipManagement.Application.DTOs.Company;
using InternshipManagement.Application.DTOs.Internship;
using InternshipManagement.Application.Interfaces;
using InternshipManagement.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InternshipManagement.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;
        private readonly ICompanyService _companyService;
        private readonly IInternshipService _internshipService;

        public AdminController(
            IAdminService adminService,
            ICompanyService companyService,
            IInternshipService internshipService)
        {
            _adminService = adminService;
            _companyService = companyService;
            _internshipService = internshipService;
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var stats = await _adminService.GetStatsAsync();
            return Ok(stats);
        }

        [HttpGet("companies")]
        public async Task<IActionResult> GetAllCompanies()
        {
            var companies = await _adminService.GetAllCompaniesAsync();
            return Ok(companies);
        }

        [HttpGet("companies/recent")]
        public async Task<IActionResult> GetRecentCompanies([FromQuery] int count = 5)
        {
            var companies = await _adminService.GetRecentCompaniesAsync(count);
            return Ok(companies);
        }

        [HttpPut("companies/{companyId}/verification")]
        public async Task<IActionResult> ReviewVerification(int companyId, [FromBody] AdminReviewVerificationRequest request)
        {
            try
            {
                var result = await _companyService.ReviewVerificationAsync(companyId, new ReviewVerificationRequest
                {
                    Approved = request.Approved,
                    Notes = request.Notes
                });
                return Ok(new { Success = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("internships")]
        public async Task<IActionResult> GetAllInternships()
        {
            var internships = await _adminService.GetAllInternshipsAsync();
            return Ok(internships);
        }

        [HttpPost("internships/{id}/moderate")]
        public async Task<IActionResult> ModerateInternship(int id, [FromBody] AdminModerateInternshipRequest request)
        {
            try
            {
                if (!Enum.TryParse<ModerationStatus>(request.Status, true, out var status))
                    return BadRequest(new { Error = $"Invalid moderation status '{request.Status}'." });

                var result = await _internshipService.ModerateInternshipAsync(id, status, request.Notes);
                return Ok(new { Success = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("reports")]
        public async Task<IActionResult> GetAllReports()
        {
            var reports = await _adminService.GetAllReportsAsync();
            return Ok(reports);
        }

        [HttpGet("reports/recent")]
        public async Task<IActionResult> GetRecentReports([FromQuery] int count = 5)
        {
            var reports = await _adminService.GetRecentReportsAsync(count);
            return Ok(reports);
        }

        [HttpPut("reports/{id}/resolve")]
        public async Task<IActionResult> ResolveReport(int id, [FromBody] ResolveReportRequest request)
        {
            var result = await _adminService.ResolveReportAsync(id, request.Response, request.Resolved);
            return Ok(new { Success = result });
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _adminService.GetAllUsersAsync();
            return Ok(users);
        }

        [HttpPost("users/{id}/suspend")]
        public async Task<IActionResult> SuspendUser(int id, [FromQuery] bool suspend = true)
        {
            var result = await _adminService.SuspendUserAsync(id, suspend);
            return Ok(new { Success = result });
        }
    }

    public class AdminReviewVerificationRequest
    {
        public bool Approved { get; set; }
        public string? Notes { get; set; }
    }

    public class AdminModerateInternshipRequest
    {
        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }
}
