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
                var result = await _companyService.ReviewVerificationAsync(
                    companyId,
                    int.Parse(User.FindFirst("userId")?.Value ?? "0"),
                    new ReviewVerificationRequest
                {
                    Approved = request.Approved,
                    Status = request.Status,
                    ReasonCode = request.ReasonCode,
                    Notes = request.Notes,
                    CriteriaAssessment = request.CriteriaAssessment
                });
                return Ok(new { Success = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("companies/{companyId}/verification/history")]
        public async Task<IActionResult> GetVerificationHistory(int companyId)
        {
            var history = await _companyService.GetVerificationHistoryAsync(companyId);
            return Ok(history.Select(entry => new
            {
                Status = entry.Status.ToString(),
                entry.ReasonCode,
                entry.Notes,
                entry.CriteriaAssessmentJson,
                entry.ChangedByUserId,
                entry.IsAppeal,
                entry.CreatedAt
            }));
        }

        [HttpGet("companies/{companyId}/verification/evidence")]
        public async Task<IActionResult> GetVerificationEvidence(int companyId)
        {
            var evidence = await _companyService.GetVerificationEvidenceAsync(companyId);
            return Ok(evidence);
        }

        [HttpPut("companies/{companyId}/verification/appeal")]
        public async Task<IActionResult> ReviewVerificationAppeal(
            int companyId,
            [FromBody] AdminReviewVerificationRequest request)
        {
            try
            {
                var result = await _companyService.ReviewVerificationAppealAsync(
                    companyId,
                    int.Parse(User.FindFirst("userId")?.Value ?? "0"),
                    new ReviewVerificationAppealRequest
                    {
                        Approved = request.Approved,
                        ReasonCode = request.ReasonCode,
                        Notes = request.Notes,
                        CriteriaAssessment = request.CriteriaAssessment
                    });
                return result ? Ok(new { Success = true }) : NotFound();
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPost("companies/verification/reverification")]
        public async Task<IActionResult> RunAgedCompanyReverification()
        {
            var updatedCount = await _companyService.UpdateAgedVerificationsAsync();
            return Ok(new { UpdatedCount = updatedCount });
        }

        [HttpGet("companies/verification/sla")]
        public async Task<IActionResult> GetVerificationSla()
        {
            var sla = await _companyService.GetVerificationSlaAsync();
            return Ok(sla);
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
            var status = string.IsNullOrWhiteSpace(request.Status)
                ? (request.Resolved ? ReportStatus.Resolved : ReportStatus.Dismissed)
                : Enum.TryParse<ReportStatus>(request.Status, true, out var parsed) && Enum.IsDefined(parsed)
                    ? parsed
                    : (ReportStatus?)null;
            if (!status.HasValue)
                return BadRequest(new { Error = "Invalid report status." });

            var result = await _adminService.ResolveReportAsync(id, request.Response,
                status.Value, request.FraudUpheld, int.Parse(User.FindFirst("userId")?.Value ?? "0"));
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
            var result = await _adminService.SuspendUserAsync(
                id, suspend, int.Parse(User.FindFirst("userId")?.Value ?? "0"));
            return Ok(new { Success = result });
        }
    }

    public class AdminReviewVerificationRequest
    {
        public bool Approved { get; set; }
        public string? Status { get; set; }
        public string? ReasonCode { get; set; }
        public string? Notes { get; set; }
        public Dictionary<string, bool>? CriteriaAssessment { get; set; }
    }

    public class AdminModerateInternshipRequest
    {
        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }
}
