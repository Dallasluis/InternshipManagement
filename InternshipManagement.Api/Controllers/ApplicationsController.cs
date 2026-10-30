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
        private readonly IInternshipLifecycleService _lifecycleService;
        private readonly IApplicationDbContext _context;

        public ApplicationsController(
            IApplicationService applicationService,
            IInternshipLifecycleService lifecycleService,
            IApplicationDbContext context)
        {
            _applicationService = applicationService;
            _lifecycleService = lifecycleService;
            _context = context;
        }

        [HttpPost]
        [Authorize(Roles = "Student")]
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
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GetMyApplications()
        {
            var userId = GetUserId();
            var applications = await _applicationService.GetStudentApplicationsAsync(userId);
            return Ok(applications);
        }

        [HttpGet("student/stats")]
        [Authorize(Roles = "Student")]
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
        [Authorize(Roles = "Company")]
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
        [Authorize(Roles = "Company")]
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
        [Authorize(Roles = "Company")]
        public async Task<IActionResult> GetInternshipApplications(int internshipId)
        {
            if (!await OwnsInternshipAsync(internshipId))
                return NotFound();

            var applications = await _applicationService.GetInternshipApplicationsAsync(internshipId);
            return Ok(applications);
        }

        [HttpGet("internship/{internshipId}/shortlisted")]
        [Authorize(Roles = "Company")]
        public async Task<IActionResult> GetShortlisted(int internshipId)
        {
            if (!await OwnsInternshipAsync(internshipId))
                return NotFound();

            var applications = await _applicationService.GetShortlistedApplicationsAsync(internshipId);
            return Ok(applications);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            if (!await CanAccessApplicationAsync(id))
                return NotFound();

            var application = await _applicationService.GetApplicationByIdAsync(id);
            if (application == null)
                return NotFound();

            return Ok(application);
        }

        [HttpGet("{id}/status-history")]
        public async Task<IActionResult> GetStatusHistory(int id)
        {
            if (!await CanAccessApplicationAsync(id))
                return NotFound();

            var history = await _lifecycleService.GetApplicationStatusHistoryAsync(id);
            return Ok(history);
        }

        [HttpPut("{id}/status")]
        [Authorize(Roles = "Company")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateApplicationStatusRequest request)
        {
            try
            {
                var userId = GetUserId();
                var result = await _applicationService.UpdateApplicationStatusAsync(userId, id, request);
                if (!result)
                    return NotFound();
                return Ok(new { Success = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPost("{id}/shortlist")]
        [Authorize(Roles = "Company")]
        public async Task<IActionResult> Shortlist(int id, [FromBody] ShortlistRequest request)
        {
            try
            {
                var userId = GetUserId();
                var result = await _applicationService.ShortlistApplicationAsync(userId, id, request.Notes);
                if (!result)
                    return NotFound();
                return Ok(new { Success = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPost("{id}/schedule-interview")]
        [Authorize(Roles = "Company")]
        public async Task<IActionResult> ScheduleInterview(int id, [FromBody] ScheduleInterviewRequest request)
        {
            try
            {
                var userId = GetUserId();
                var result = await _applicationService.ScheduleInterviewAsync(userId, id, request);
                if (!result)
                    return NotFound();
                return Ok(new { Success = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPost("{id}/mark-interview-completed")]
        [Authorize(Roles = "Company")]
        public async Task<IActionResult> MarkInterviewCompleted(int id)
        {
            try
            {
                var userId = GetUserId();
                var result = await _applicationService.MarkInterviewCompletedAsync(userId, id);
                if (!result)
                    return NotFound();
                return Ok(new { Success = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPost("{id}/make-offer")]
        [Authorize(Roles = "Company")]
        public async Task<IActionResult> MakeOffer(int id, [FromBody] MakeOfferRequest request)
        {
            try
            {
                var userId = GetUserId();
                var result = await _applicationService.MakeOfferAsync(userId, id, request);
                if (!result)
                    return NotFound();
                return Ok(new { Success = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPost("{id}/respond-to-offer")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> RespondToOffer(int id, [FromBody] RespondToOfferRequest request)
        {
            try
            {
                var userId = GetUserId();
                var result = await _applicationService.RespondToOfferAsync(userId, id, request.Accepted);
                if (!result)
                    return NotFound();
                return Ok(new { Success = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPost("{id}/withdraw")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Withdraw(int id)
        {
            try
            {
                var userId = GetUserId();
                var result = await _applicationService.WithdrawApplicationAsync(userId, id);
                if (!result)
                    return NotFound();
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

        private Task<bool> OwnsInternshipAsync(int internshipId)
        {
            var userId = GetUserId();
            return _context.Internships.AnyAsync(i =>
                i.Id == internshipId &&
                !i.IsDeleted &&
                !i.CompanyProfile.IsDeleted &&
                i.CompanyProfile.UserId == userId);
        }

        private Task<bool> CanAccessApplicationAsync(int applicationId)
        {
            var userId = GetUserId();
            var isStudent = User.IsInRole("Student");
            var isCompany = User.IsInRole("Company");

            return _context.InternshipApplications.AnyAsync(a =>
                a.Id == applicationId &&
                !a.IsDeleted &&
                ((isStudent && a.StudentProfile.UserId == userId) ||
                 (isCompany && a.Internship.CompanyProfile.UserId == userId)));
        }
    }
}
