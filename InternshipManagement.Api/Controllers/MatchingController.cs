using InternshipManagement.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InternshipManagement.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MatchingController : ControllerBase
    {
        private readonly IMatchingService _matchingService;

        public MatchingController(IMatchingService matchingService)
        {
            _matchingService = matchingService;
        }

        [HttpGet("student/internships")]
        public async Task<IActionResult> RecommendInternshipsForStudent([FromQuery] int maxResults = 10)
        {
            var userId = GetUserId();
            var recommendations = await _matchingService.RecommendInternshipsForStudentAsync(userId, maxResults);
            return Ok(recommendations);
        }

        [HttpGet("company/internships/{internshipId}/students")]
        public async Task<IActionResult> RecommendStudentsForInternship(int internshipId, [FromQuery] int maxResults = 10)
        {
            var userId = GetUserId();
            var recommendations = await _matchingService.RecommendStudentsForInternshipAsync(userId, internshipId, maxResults);
            return Ok(recommendations);
        }

        private int GetUserId()
        {
            return int.Parse(User.FindFirst("userId")?.Value ?? "0");
        }
    }
}
