using InternshipManagement.Application.DTOs.Application;
using InternshipManagement.Application.Interfaces;
using InternshipManagement.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InternshipManagement.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ApplicationsController : ControllerBase
    {
        private readonly IApplicationService _applicationService;
        private readonly IApplicationDbContext _context;

        public ApplicationsController(IApplicationService applicationService, IApplicationDbContext context)
        {
            _applicationService = applicationService;
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> Apply([FromBody] ApplyRequest request)
        {
            try
            {
                var userId = GetUserId();
                var result = await _applicationService.ApplyAsync(userId, request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("student")]
        public async Task<IActionResult> GetMyApplications()
        {
            var userId = GetUserId();
            var applications = await _applicationService.GetStudentApplicationsAsync(userId);
            return Ok(applications);
        }

        [HttpGet("student/stats")]
        public async Task<IActionResult> GetStudentStats()
        {
            var userId = GetUserId();
            var studentProfile = await _context.StudentProfiles.FirstOrDefaultAsync(s => s.UserId == userId);

            if (studentProfile == null)
                return Ok(new { Total = 0, Pending = 0, Shortlisted = 0, Rejected = 0 });

            var applications = _context.InternshipApplications
                .Where(a => a.StudentProfileId == studentProfile.Id && !a.IsDeleted);

            return Ok(new
            {
                Total = await applications.CountAsync(),
                Pending = await applications.CountAsync(a => a.Status == ApplicationStatus.Applied || a.Status == ApplicationStatus.UnderReview),
                Shortlisted = await applications.CountAsync(a => a.Status == ApplicationStatus.Shortlisted || a.IsShortlisted),
                Rejected = await applications.CountAsync(a => a.Status == ApplicationStatus.Rejected)
            });
        }

        [HttpGet("company")]
        public async Task<IActionResult> GetCompanyApplications()
        {
            var userId = GetUserId();
            var companyProfile = await _context.CompanyProfiles.FirstOrDefaultAsync(c => c.UserId == userId && !c.IsDeleted);

            if (companyProfile == null)
                return Ok(new List<ApplicationResponse>());

            var internshipIds = await _context.Internships
                .Where(i => i.CompanyProfileId == companyProfile.Id && !i.IsDeleted)
                .Select(i => i.Id)
                .ToListAsync();

            var responses = new List<ApplicationResponse>();
            foreach (var internshipId in internshipIds)
            {
                responses.AddRange(await _applicationService.GetInternshipApplicationsAsync(internshipId));
            }

            return Ok(responses.OrderByDescending(a => a.AppliedAt));
        }

        [HttpGet("company/stats")]
        public async Task<IActionResult> GetCompanyStats()
        {
            var userId = GetUserId();
            var companyProfile = await _context.CompanyProfiles.FirstOrDefaultAsync(c => c.UserId == userId && !c.IsDeleted);

            if (companyProfile == null)
                return Ok(new { TotalInternships = 0, ActiveInternships = 0, TotalApplications = 0, ShortlistedCount = 0 });

            var internships = _context.Internships.Where(i => i.CompanyProfileId == companyProfile.Id && !i.IsDeleted);
            var internshipIds = await internships.Select(i => i.Id).ToListAsync();
            var applications = _context.InternshipApplications.Where(a => internshipIds.Contains(a.InternshipId) && !a.IsDeleted);

            return Ok(new
            {
                TotalInternships = await internships.CountAsync(),
                ActiveInternships = await internships.CountAsync(i => i.Status == InternshipStatus.Published),
                TotalApplications = await applications.CountAsync(),
                ShortlistedCount = await applications.CountAsync(a => a.Status == ApplicationStatus.Shortlisted || a.IsShortlisted)
            });
        }

        [HttpGet("internship/{internshipId}")]
        public async Task<IActionResult> GetInternshipApplications(int internshipId)
        {
            var applications = await _applicationService.GetInternshipApplicationsAsync(internshipId);
            return Ok(applications);
        }

        [HttpGet("internship/{internshipId}/shortlisted")]
        public async Task<IActionResult> GetShortlisted(int internshipId)
        {
            var applications = await _applicationService.GetShortlistedApplicationsAsync(internshipId);
            return Ok(applications);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var application = await _applicationService.GetApplicationByIdAsync(id);
            if (application == null)
                return NotFound();

            return Ok(application);
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateApplicationStatusRequest request)
        {
            try
            {
                var result = await _applicationService.UpdateApplicationStatusAsync(id, request);
                return Ok(new { Success = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPost("{id}/shortlist")]
        public async Task<IActionResult> Shortlist(int id, [FromBody] ShortlistRequest request)
        {
            try
            {
                var result = await _applicationService.ShortlistApplicationAsync(id, request.Notes);
                return Ok(new { Success = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPost("{id}/schedule-interview")]
        public async Task<IActionResult> ScheduleInterview(int id, [FromBody] ScheduleInterviewRequest request)
        {
            try
            {
                var result = await _applicationService.ScheduleInterviewAsync(id, request);
                return Ok(new { Success = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPost("{id}/mark-interview-completed")]
        public async Task<IActionResult> MarkInterviewCompleted(int id)
        {
            try
            {
                var result = await _applicationService.MarkInterviewCompletedAsync(id);
                return Ok(new { Success = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPost("{id}/make-offer")]
        public async Task<IActionResult> MakeOffer(int id, [FromBody] MakeOfferRequest request)
        {
            try
            {
                var result = await _applicationService.MakeOfferAsync(id, request);
                return Ok(new { Success = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPost("{id}/respond-to-offer")]
        public async Task<IActionResult> RespondToOffer(int id, [FromBody] RespondToOfferRequest request)
        {
            try
            {
                var result = await _applicationService.RespondToOfferAsync(id, request.Accepted);
                return Ok(new { Success = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPost("{id}/withdraw")]
        public async Task<IActionResult> Withdraw(int id)
        {
            try
            {
                var result = await _applicationService.WithdrawApplicationAsync(id);
                return Ok(new { Success = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        private int GetUserId()
        {
            return int.Parse(User.FindFirst("userId")?.Value ?? "0");
        }
    }
}
