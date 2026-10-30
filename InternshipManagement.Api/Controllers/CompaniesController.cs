using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using InternshipManagement.Application.DTOs.Company;
using InternshipManagement.Application.Interfaces;
using System.Security.Claims;

namespace InternshipManagement.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CompaniesController : ControllerBase
    {
        private readonly ICompanyService _companyService;

        public CompaniesController(ICompanyService companyService)
        {
            _companyService = companyService;
        }

        [HttpGet("profile")]
        [Authorize(Roles = "Company")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = int.Parse(User.FindFirst("userId")?.Value ?? "0");
            var profile = await _companyService.GetCompanyProfileAsync(userId);

            if (profile == null)
                return NotFound(new { Error = "Company profile not found" });

            return Ok(profile);
        }

        [HttpPut("profile")]
        [Authorize(Roles = "Company")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateCompanyProfileRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst("userId")?.Value ?? "0");
                var result = await _companyService.UpdateCompanyProfileAsync(userId, request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPost("verification")]
        [Authorize(Roles = "Company")]
        public async Task<IActionResult> SubmitVerification([FromBody] SubmitVerificationRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst("userId")?.Value ?? "0");
                var result = await _companyService.SubmitVerificationAsync(userId, request);
                return Ok(new { Success = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPost("verification/appeal")]
        [Authorize(Roles = "Company")]
        public async Task<IActionResult> SubmitVerificationAppeal([FromBody] SubmitVerificationAppealRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst("userId")?.Value ?? "0");
                var submitted = await _companyService.SubmitVerificationAppealAsync(userId, request);
                return submitted ? Ok(new { Success = true }) : NotFound();
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("verification/history")]
        [Authorize(Roles = "Company")]
        public async Task<IActionResult> GetVerificationHistory()
        {
            var userId = int.Parse(User.FindFirst("userId")?.Value ?? "0");
            var profile = await _companyService.GetCompanyProfileAsync(userId);
            if (profile == null)
                return NotFound();
            var history = await _companyService.GetVerificationHistoryAsync(profile.Id);
            return Ok(history.Select(entry => new
            {
                Status = entry.Status.ToString(),
                entry.ReasonCode,
                entry.Notes,
                entry.IsAppeal,
                entry.CreatedAt
            }));
        }

        [HttpPut("{companyProfileId}/subscription")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateSubscription(int companyProfileId, [FromBody] UpdateSubscriptionRequest request)
        {
            try
            {
                var result = await _companyService.UpdateSubscriptionStatusAsync(companyProfileId, request.IsSubscribed);
                return Ok(new { Success = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }
    }
}