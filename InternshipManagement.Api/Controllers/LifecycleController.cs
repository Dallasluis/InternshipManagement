using InternshipManagement.Application.DTOs.Lifecycle;
using InternshipManagement.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InternshipManagement.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class LifecycleController : ControllerBase
    {
        private readonly IInternshipLifecycleService _lifecycleService;

        public LifecycleController(IInternshipLifecycleService lifecycleService)
        {
            _lifecycleService = lifecycleService;
        }

        [HttpGet("placements/student")]
        public async Task<IActionResult> GetStudentPlacements()
        {
            return Ok(await _lifecycleService.GetStudentPlacementsAsync(GetUserId()));
        }

        [HttpGet("placements/company")]
        public async Task<IActionResult> GetCompanyPlacements()
        {
            return Ok(await _lifecycleService.GetCompanyPlacementsAsync(GetUserId()));
        }

        [HttpGet("placements/{id}")]
        public async Task<IActionResult> GetPlacement(int id)
        {
            var placement = await _lifecycleService.GetPlacementAsync(id);
            return placement == null ? NotFound() : Ok(placement);
        }

        [HttpPost("placements/{id}/progress-reports")]
        public async Task<IActionResult> SubmitProgressReport(int id, [FromBody] SubmitProgressReportRequest request)
        {
            try
            {
                var report = await _lifecycleService.SubmitProgressReportAsync(GetUserId(), id, request);
                return Ok(report);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("placements/{id}/progress-reports")]
        public async Task<IActionResult> GetProgressReports(int id)
        {
            return Ok(await _lifecycleService.GetProgressReportsAsync(id));
        }

        [HttpPut("progress-reports/{id}/review")]
        public async Task<IActionResult> ReviewProgressReport(int id, [FromBody] ReviewProgressReportRequest request)
        {
            try
            {
                var result = await _lifecycleService.ReviewProgressReportAsync(GetUserId(), id, request);
                return Ok(new { Success = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPost("placements/{id}/extensions")]
        public async Task<IActionResult> ProposeExtension(int id, [FromBody] ProposeExtensionRequest request)
        {
            try
            {
                var extension = await _lifecycleService.ProposeExtensionAsync(GetUserId(), id, request);
                return Ok(extension);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPut("extensions/{id}/respond")]
        public async Task<IActionResult> RespondToExtension(int id, [FromBody] RespondToExtensionRequest request)
        {
            try
            {
                var result = await _lifecycleService.RespondToExtensionAsync(GetUserId(), id, request);
                return Ok(new { Success = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPost("placements/{id}/withdrawals")]
        public async Task<IActionResult> RequestWithdrawal(int id, [FromBody] RequestWithdrawalRequest request)
        {
            try
            {
                var withdrawal = await _lifecycleService.RequestWithdrawalAsync(GetUserId(), id, request);
                return Ok(withdrawal);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPut("withdrawals/{id}/review")]
        public async Task<IActionResult> ReviewWithdrawal(int id, [FromBody] ReviewWithdrawalRequest request)
        {
            try
            {
                var result = await _lifecycleService.ReviewWithdrawalAsync(GetUserId(), id, request);
                return Ok(new { Success = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPut("placements/{id}/complete")]
        public async Task<IActionResult> CompletePlacement(int id, [FromBody] CompletePlacementRequest request)
        {
            try
            {
                var result = await _lifecycleService.CompletePlacementAsync(GetUserId(), id, request);
                return Ok(new { Success = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPut("placements/{id}/terminate")]
        public async Task<IActionResult> TerminatePlacement(int id, [FromBody] TerminatePlacementRequest request)
        {
            try
            {
                var result = await _lifecycleService.TerminatePlacementAsync(GetUserId(), id, request);
                return Ok(new { Success = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPost("placements/{id}/evaluations")]
        public async Task<IActionResult> SubmitEvaluation(int id, [FromBody] SubmitEvaluationRequest request)
        {
            try
            {
                var evaluation = await _lifecycleService.SubmitEvaluationAsync(GetUserId(), id, request);
                return Ok(evaluation);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("placements/{id}/evaluations")]
        public async Task<IActionResult> GetEvaluations(int id)
        {
            return Ok(await _lifecycleService.GetEvaluationsAsync(id));
        }

        private int GetUserId()
        {
            return int.Parse(User.FindFirst("userId")?.Value ?? "0");
        }
    }
}
