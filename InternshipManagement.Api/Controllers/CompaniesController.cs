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
        public async Task<IActionResult> GetProfile()
        {
            var userId = int.Parse(User.FindFirst("userId")?.Value ?? "0");
            var profile = await _companyService.GetCompanyProfileAsync(userId);

            if (profile == null)
                return NotFound(new { Error = "Company profile not found" });

            return Ok(profile);
        }

        [HttpPut("profile")]
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

        // Admin endpoints
        [HttpPut("verification/{companyProfileId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ReviewVerification(int companyProfileId, [FromBody] ReviewVerificationRequest request)
        {
            try
            {
                var result = await _companyService.ReviewVerificationAsync(companyProfileId, request);
                return Ok(new { Success = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
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