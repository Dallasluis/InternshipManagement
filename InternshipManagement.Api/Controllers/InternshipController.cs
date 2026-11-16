using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using InternshipManagement.Application.DTOs.Internship;
using InternshipManagement.Application.Interfaces;
using System.Security.Claims;

namespace InternshipManagement.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Route("api/Internship")]
    public class InternshipsController : ControllerBase
    {
        private readonly IInternshipService _internshipService;
        private readonly ICompanyService _companyService;

        public InternshipsController(IInternshipService internshipService, ICompanyService companyService)
        {
            _internshipService = internshipService;
            _companyService = companyService;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Search([FromQuery] InternshipSearchRequest request)
        {
            var result = await _internshipService.SearchInternshipsAsync(request);
            return Ok(new
            {
                Items = result.Items,
                TotalCount = result.TotalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            });
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(int id)
        {
            var userId = int.TryParse(User.FindFirst("userId")?.Value, out var parsedUserId)
                ? parsedUserId
                : (int?)null;
            var internship = await _internshipService.GetInternshipByIdAsync(id, userId);
            if (internship == null)
                return NotFound();

            return Ok(internship);
        }

        [HttpPost]
        [Authorize(Roles = "Company")]
        public async Task<IActionResult> Create([FromBody] CreateInternshipRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst("userId")?.Value ?? "0");
                var result = await _internshipService.CreateInternshipAsync(userId, request);
                return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Company")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateInternshipRequest request)
        {
            try
            {
                request.Id = id;
                var userId = int.Parse(User.FindFirst("userId")?.Value ?? "0");
                var result = await _internshipService.UpdateInternshipAsync(userId, id, request);
                if (result == null)
                    return NotFound();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPost("{id}/publish")]
        [Authorize(Roles = "Company")]
        public async Task<IActionResult> Publish(int id)
        {
            try
            {
                var userId = int.Parse(User.FindFirst("userId")?.Value ?? "0");
                var result = await _internshipService.PublishInternshipAsync(userId, id);
                if (!result)
                    return NotFound();
                return Ok(new { Success = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPost("{id}/close")]
        [Authorize(Roles = "Company")]
        public async Task<IActionResult> Close(int id)
        {
            try
            {
                var userId = int.Parse(User.FindFirst("userId")?.Value ?? "0");
                var result = await _internshipService.CloseInternshipAsync(userId, id);
                if (!result)
                    return NotFound();
                return Ok(new { Success = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Company")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = int.Parse(User.FindFirst("userId")?.Value ?? "0");
            var result = await _internshipService.DeleteInternshipAsync(userId, id);
            if (!result)
                return NotFound();
            return Ok(new { Success = result });
        }

        [HttpGet("company")]
        [Authorize(Roles = "Company")]
        public async Task<IActionResult> GetCompanyInternships()
        {
            try
            {
                var userId = int.Parse(User.FindFirst("userId")?.Value ?? "0");
                var companyProfile = await _companyService.GetCompanyProfileAsync(userId);

                if (companyProfile == null)
                    return NotFound(new { Error = "Company profile not found" });

                var internships = await _internshipService.GetCompanyInternshipsAsync(companyProfile.Id);
                return Ok(internships);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPost("{id}/moderate")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Moderate(int id, [FromBody] ModerateInternshipRequest request)
        {
            try
            {
                var result = await _internshipService.ModerateInternshipAsync(id, request.Status, request.Notes);
                return Ok(new { Success = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }
    }
}