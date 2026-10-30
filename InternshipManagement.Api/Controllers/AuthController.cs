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
            return result.Succeeded
                ? Ok(new { Success = true, Message = "Confirm the new email address to complete the change. Your current email remains active until then." })
                : BadRequest(result);
        }

        [HttpPost("resend-verification")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> ResendVerification()
        {
            var result = await _authService.ResendVerificationAsync(CurrentUserId, HttpContext.Connection.RemoteIpAddress?.ToString());
            if (result.Success)
                return Ok(new { Success = true, Message = "A verification link has been sent if the account needs verification." });

            return result.Error?.Contains("limit", StringComparison.OrdinalIgnoreCase) == true
                ? StatusCode(StatusCodes.Status429TooManyRequests, new { Success = false, Message = result.Error })
                : BadRequest(new { Success = false, Message = result.Error });
        }

        [HttpPost("security-code")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> SendSecurityCode()
        {
            var result = await _authService.SendSecurityCodeAsync(CurrentUserId);
            return result.Success
                ? Ok(result)
                : StatusCode(result.Message?.Contains("wait", StringComparison.OrdinalIgnoreCase) == true
                    ? StatusCodes.Status429TooManyRequests : StatusCodes.Status400BadRequest, result);
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

        [HttpPut("profile-picture")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> UpdateProfilePicture([FromBody] UpdateProfilePictureRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.ProfilePictureUrl)
                || !request.ProfilePictureUrl.StartsWith("/uploads/profile-pictures/", StringComparison.Ordinal))
            {
                return BadRequest(new { Success = false, Errors = new[] { "The profile picture path is invalid." } });
            }

            var result = await _authService.UpdateProfilePictureUrlAsync(CurrentUserId, request.ProfilePictureUrl);
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
        [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("login")]
        public async Task<IActionResult> Login([FromBody] Application.DTOs.Auth.LoginRequest request)
        {
            request.ClientIpAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
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
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
        {
            var result = await _authService.LogoutAsync(CurrentUserId);
            return Ok(new { Success = result });
        }

        [HttpPost("verify-email")]
        public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request)
        {
            var result = await _authService.VerifyEmailAsync(request.UserId, request.Token);
            return result ? Ok(new { Success = true }) : BadRequest(new { Success = false, Message = "The verification link is invalid, expired, or already used." });
        }

        [HttpGet("verify-email")]
        public async Task<IActionResult> VerifyEmailFromLink([FromQuery] int userId, [FromQuery] string token)
        {
            var result = await _authService.VerifyEmailAsync(userId, token);
            return result
                ? Content("Your email address is verified. You can now sign in.", "text/plain")
                : BadRequest("This verification link is invalid, expired, or already used.");
        }

        [HttpGet("confirm-email-change")]
        public async Task<IActionResult> ConfirmEmailChange([FromQuery] int userId, [FromQuery] string token)
        {
            var result = await _authService.ConfirmEmailChangeAsync(userId, token, HttpContext.Connection.RemoteIpAddress?.ToString());
            return result
                ? Content("Your email address has been changed successfully.", "text/plain")
                : BadRequest("This email-change link is invalid, expired, or already used.");
        }

        [HttpGet("cancel-email-change")]
        public async Task<IActionResult> CancelEmailChange([FromQuery] int userId, [FromQuery] string token)
        {
            var result = await _authService.CancelEmailChangeAsync(userId, token, HttpContext.Connection.RemoteIpAddress?.ToString());
            return result
                ? Content("The email change request has been cancelled.", "text/plain")
                : BadRequest("This cancellation link is invalid, expired, or already used.");
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

        public class UpdateProfilePictureRequest
        {
            public string ProfilePictureUrl { get; set; } = string.Empty;
        }

        public sealed class EnableTwoFactorRequest
        {
            [System.ComponentModel.DataAnnotations.Required]
            public string Code { get; set; } = string.Empty;
        }
    }
}