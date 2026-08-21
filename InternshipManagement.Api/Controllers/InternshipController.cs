using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using InternshipManagement.Application.DTOs.Internship;
using InternshipManagement.Application.Interfaces;
using System.Security.Claims;

namespace InternshipManagement.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
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
            var internship = await _internshipService.GetInternshipByIdAsync(id);
            if (internship == null)
                return NotFound();

            return Ok(internship);
        }

        [HttpPost]
        [Authorize]
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
        [Authorize]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateInternshipRequest request)
        {
            try
            {
                request.Id = id;
                var result = await _internshipService.UpdateInternshipAsync(id, request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPost("{id}/publish")]
        [Authorize]
        public async Task<IActionResult> Publish(int id)
        {
            try
            {
                var result = await _internshipService.PublishInternshipAsync(id);
                return Ok(new { Success = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPost("{id}/close")]
        [Authorize]
        public async Task<IActionResult> Close(int id)
        {
            try
            {
                var result = await _internshipService.CloseInternshipAsync(id);
                return Ok(new { Success = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _internshipService.DeleteInternshipAsync(id);
            return Ok(new { Success = result });
        }

        [HttpGet("company")]
        [Authorize]
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