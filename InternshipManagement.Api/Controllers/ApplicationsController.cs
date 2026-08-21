using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using InternshipManagement.Application.DTOs.Application;
using InternshipManagement.Application.Interfaces;
using System.Security.Claims;

namespace InternshipManagement.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ApplicationsController : ControllerBase
    {
        private readonly IApplicationService _applicationService;

        public ApplicationsController(IApplicationService applicationService)
        {
            _applicationService = applicationService;
        }

        [HttpPost]
        public async Task<IActionResult> Apply([FromBody] ApplyRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst("userId")?.Value ?? "0");
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
            var userId = int.Parse(User.FindFirst("userId")?.Value ?? "0");
            var applications = await _applicationService.GetStudentApplicationsAsync(userId);
            return Ok(applications);
        }

        [HttpGet("internship/{internshipId}")]
        public async Task<IActionResult> GetInternshipApplications(int internshipId)
        {
            var applications = await _applicationService.GetInternshipApplicationsAsync(internshipId);
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

        [HttpGet("internship/{internshipId}/shortlisted")]
        public async Task<IActionResult> GetShortlisted(int internshipId)
        {
            var applications = await _applicationService.GetShortlistedApplicationsAsync(internshipId);
            return Ok(applications);
        }
    }
}