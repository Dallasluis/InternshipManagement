using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using InternshipManagement.Application.DTOs.Student;
using InternshipManagement.Application.Interfaces;
using System.Security.Claims;

namespace InternshipManagement.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class StudentsController : ControllerBase
    {
        private readonly IStudentService _studentService;

        public StudentsController(IStudentService studentService)
        {
            _studentService = studentService;
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = int.Parse(User.FindFirst("userId")?.Value ?? "0");
            var profile = await _studentService.GetStudentProfileAsync(userId);

            if (profile == null)
                return NotFound(new { Error = "Student profile not found" });

            return Ok(profile);
        }

        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateStudentProfileRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst("userId")?.Value ?? "0");
                var result = await _studentService.UpdateStudentProfileAsync(userId, request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPost("resume")]
        public async Task<IActionResult> UploadResume([FromBody] UploadResumeRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst("userId")?.Value ?? "0");
                var result = await _studentService.UploadResumeAsync(userId, request.ResumeUrl);
                return Ok(new { Success = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPost("education")]
        public async Task<IActionResult> AddEducation([FromBody] AddEducationRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst("userId")?.Value ?? "0");
                var result = await _studentService.AddEducationAsync(userId, request);
                return Ok(new { Success = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPut("education/{educationId}")]
        public async Task<IActionResult> UpdateEducation(int educationId, [FromBody] UpdateEducationRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst("userId")?.Value ?? "0");
                var result = await _studentService.UpdateEducationAsync(userId, educationId, request);
                return Ok(new { Success = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpDelete("education/{educationId}")]
        public async Task<IActionResult> DeleteEducation(int educationId)
        {
            try
            {
                var userId = int.Parse(User.FindFirst("userId")?.Value ?? "0");
                var result = await _studentService.DeleteEducationAsync(userId, educationId);
                return Ok(new { Success = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPost("experience")]
        public async Task<IActionResult> AddExperience([FromBody] AddWorkExperienceRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst("userId")?.Value ?? "0");
                var result = await _studentService.AddWorkExperienceAsync(userId, request);
                return Ok(new { Success = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPut("experience/{experienceId}")]
        public async Task<IActionResult> UpdateExperience(int experienceId, [FromBody] UpdateWorkExperienceRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst("userId")?.Value ?? "0");
                var result = await _studentService.UpdateWorkExperienceAsync(userId, experienceId, request);
                return Ok(new { Success = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpDelete("experience/{experienceId}")]
        public async Task<IActionResult> DeleteExperience(int experienceId)
        {
            try
            {
                var userId = int.Parse(User.FindFirst("userId")?.Value ?? "0");
                var result = await _studentService.DeleteWorkExperienceAsync(userId, experienceId);
                return Ok(new { Success = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPost("skill")]
        public async Task<IActionResult> AddSkill([FromBody] AddSkillRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst("userId")?.Value ?? "0");
                var result = await _studentService.AddSkillAsync(userId, request);
                return Ok(new { Success = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpDelete("skill/{skillId}")]
        public async Task<IActionResult> DeleteSkill(int skillId)
        {
            try
            {
                var userId = int.Parse(User.FindFirst("userId")?.Value ?? "0");
                var result = await _studentService.DeleteSkillAsync(userId, skillId);
                return Ok(new { Success = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }
    }
}