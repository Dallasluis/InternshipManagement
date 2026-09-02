using InternshipManagement.Application.DTOs.Auth;
using InternshipManagement.Application.Interfaces;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace InternshipManagement.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        private int CurrentUserId => int.Parse(User.FindFirstValue("userId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("change-password")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var result = await _authService.ChangePasswordAsync(CurrentUserId, request);
            return result.Succeeded ? Ok(new { Success = true }) : BadRequest(result);
        }

        [HttpPost("change-email")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> ChangeEmail([FromBody] ChangeEmailRequest request)
        {
            var result = await _authService.ChangeEmailAsync(CurrentUserId, request);
            return result.Succeeded ? Ok(new { Success = true, Message = "Email changed. Please log in again." }) : BadRequest(result);
        }

        [HttpGet("preferences")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> GetPreferences()
        {
            return Ok(await _authService.GetAccountPreferencesAsync(CurrentUserId));
        }

        [HttpPut("preferences")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> UpdatePreferences([FromBody] UpdateAccountPreferencesRequest request)
        {
            var result = await _authService.UpdateAccountPreferencesAsync(CurrentUserId, request);
            return result.Succeeded ? Ok(new { Success = true }) : BadRequest(result);
        }

        [HttpPost("deactivate")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> Deactivate()
        {
            var result = await _authService.DeactivateAccountAsync(CurrentUserId);
            return result.Succeeded ? Ok(new { Success = true }) : BadRequest(result);
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] Application.DTOs.Auth.RegisterRequest request)
        {
            var result = await _authService.RegisterAsync(request);
            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] Application.DTOs.Auth.LoginRequest request)
        {
            var result = await _authService.LoginAsync(request);
            if (!result.Success)
                return Unauthorized(result);

            return Ok(result);
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            var result = await _authService.RefreshTokenAsync(request.RefreshToken);
            if (!result.Success)
                return Unauthorized(result);

            return Ok(result);
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
        {
            var result = await _authService.LogoutAsync(request.UserId);
            return Ok(new { Success = result });
        }

        [HttpPost("verify-email")]
        public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request)
        {
            var result = await _authService.VerifyEmailAsync(request.UserId, request.Token);
            return Ok(new { Success = result });
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] Application.DTOs.Auth.ForgotPasswordRequest request)
        {
            var result = await _authService.RequestPasswordResetAsync(request.Email);
            return Ok(new { Success = result });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] Application.DTOs.Auth.ResetPasswordRequest request)
        {
            var result = await _authService.ResetPasswordAsync(request.Email, request.Token, request.NewPassword);
            return Ok(new { Success = result });
        }
    }
}