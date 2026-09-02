using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using InternshipManagement.Application.DTOs.Auth;
using InternshipManagement.Application.Interfaces;
using InternshipManagement.Domain.Entities;
using InternshipManagement.Domain.Enums;
using Microsoft.IdentityModel.Tokens;

namespace InternshipManagement.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IApplicationDbContext _context;
        private readonly IIdentityService _identityService;

        public AuthService(IApplicationDbContext context, IIdentityService identityService)
        {
            _context = context;
            _identityService = identityService;
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            // Check if user exists
            var existingUser = await _identityService.FindUserByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return new AuthResponse
                {
                    Success = false,
                    Errors = new List<string> { "User with this email already exists" }
                };
            }

            // Create user using IdentityService
            var result = await _identityService.CreateUserAsync(
                request.Email,
                request.Password,
                request.FirstName,
                request.LastName,
                request.UserType ?? "Student"
            );

            if (!result.Succeeded)
            {
                return new AuthResponse
                {
                    Success = false,
                    Errors = result.Errors
                };
            }

            // Assign role
            var role = request.UserType == "Company" ? "Company" : request.UserType == "Admin" ? "Admin" : "Student";
            if (!await _identityService.RoleExistsAsync(role))
            {
                await _identityService.CreateRoleAsync(role);
            }
            await _identityService.AddToRoleAsync(result.UserId, role);

            // Create profile based on user type (only for Company and Student)
            var userId = int.Parse(result.UserId);
            if (request.UserType == "Company")
            {
                var companyProfile = new CompanyProfile
                {
                    UserId = userId,
                    CompanyName = request.CompanyName ?? $"{request.FirstName} {request.LastName}'s Company",
                    Industry = request.Industry ?? string.Empty,
                    VerificationStatus = CompanyVerificationStatus.Pending,
                    IsSubscribed = false
                };
                _context.CompanyProfiles.Add(companyProfile);
            }
            else if (request.UserType == "Student" || string.IsNullOrEmpty(request.UserType))
            {
                var studentProfile = new StudentProfile
                {
                    UserId = userId,
                    FirstName = request.FirstName,
                    LastName = request.LastName
                };
                _context.StudentProfiles.Add(studentProfile);
            }

            await _context.SaveChangesAsync();

            // Generate email verification token
            var emailToken = await _identityService.GenerateEmailConfirmationTokenAsync(result.UserId);
            // TODO: Send verification email

            return new AuthResponse
            {
                Success = true,
                UserId = result.UserId,
                UserType = request.UserType ?? "Student",
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Message = "Registration successful. Please verify your email."
            };
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            var user = await _identityService.FindUserByEmailAsync(request.Email);
            if (user == null)
            {
                return new AuthResponse
                {
                    Success = false,
                    Errors = new List<string> { "Invalid email or password" }
                };
            }

            var signInResult = await _identityService.SignInAsync(request.Email, request.Password, false);
            if (!signInResult.Succeeded)
            {
                if (signInResult.IsLockedOut)
                {
                    return new AuthResponse
                    {
                        Success = false,
                        Errors = new List<string> { "Your account has been locked out." }
                    };
                }
                return new AuthResponse
                {
                    Success = false,
                    Errors = new List<string> { "Invalid email or password" }
                };
            }

            if (!user.IsActive)
            {
                return new AuthResponse
                {
                    Success = false,
                    Errors = new List<string> { "Your account has been deactivated" }
                };
            }

            var token = await _identityService.GenerateJwtTokenAsync(user.Id.ToString());
            var refreshToken = _identityService.GenerateRefreshToken();

            await _identityService.UpdateRefreshTokenAsync(
                user.Id.ToString(),
                refreshToken,
                DateTime.UtcNow.AddDays(7)
            );

            return new AuthResponse
            {
                Success = true,
                Token = token,
                RefreshToken = refreshToken,
                UserId = user.Id.ToString(),
                UserType = user.UserType,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName
            };
        }

        public async Task<AuthResponse> RefreshTokenAsync(string refreshToken)
        {
            // Find user with the refresh token
            var user = await _identityService.FindUserByEmailAsync(refreshToken);
            // Note: This needs to be implemented differently since we need to find by refresh token
            // For MVP, we'll handle this differently
            return new AuthResponse
            {
                Success = false,
                Errors = new List<string> { "Refresh token not implemented yet" }
            };
        }

        public async Task<bool> LogoutAsync(int userId)
        {
            await _identityService.UpdateRefreshTokenAsync(
                userId.ToString(),
                null,
                DateTime.MinValue // or DateTime.UtcNow depending on semantics
            );
            await _identityService.SignOutAsync();
            return true;
        }

        public async Task<bool> VerifyEmailAsync(int userId, string token)
        {
            var result = await _identityService.ConfirmEmailAsync(userId.ToString(), token);
            return result.Succeeded;
        }

        public async Task<bool> RequestPasswordResetAsync(string email)
        {
            var user = await _identityService.FindUserByEmailAsync(email);
            if (user == null) return false;

            var token = await _identityService.GeneratePasswordResetTokenAsync(user.Id.ToString());
            // TODO: Send reset email
            return true;
        }

        public async Task<bool> ResetPasswordAsync(string email, string token, string newPassword)
        {
            var user = await _identityService.FindUserByEmailAsync(email);
            if (user == null) return false;

            var result = await _identityService.ResetPasswordAsync(
                user.Id.ToString(),
                token,
                newPassword
            );
            return result.Succeeded;
        }

        public Task<IdentityResult> ChangePasswordAsync(int userId, ChangePasswordRequest request) =>
            _identityService.ChangePasswordAsync(userId.ToString(), request.CurrentPassword, request.NewPassword);

        public Task<IdentityResult> ChangeEmailAsync(int userId, ChangeEmailRequest request) =>
            _identityService.ChangeEmailAsync(userId.ToString(), request.CurrentPassword, request.NewEmail);

        public Task<AccountPreferencesDto> GetAccountPreferencesAsync(int userId) =>
            _identityService.GetAccountPreferencesAsync(userId.ToString());

        public Task<IdentityResult> UpdateAccountPreferencesAsync(int userId, UpdateAccountPreferencesRequest request) =>
            _identityService.UpdateAccountPreferencesAsync(userId.ToString(), new AccountPreferencesDto
            {
                EmailNotifications = request.EmailNotifications,
                InternshipAlerts = request.InternshipAlerts
            });

        public Task<IdentityResult> DeactivateAccountAsync(int userId) =>
            _identityService.DeactivateAccountAsync(userId.ToString());

        // Helper methods
        private static string GenerateJwtToken(string userId, string email, string userType)
        {
            // This should be in IdentityService but kept here for completeness
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Email, email),
                new Claim("userId", userId),
                new Claim("userType", userType)
            };

            // In real implementation, this would use configuration
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("YourSuperSecretKeyHere1234567890!"));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: "InternshipManagementAPI",
                audience: "InternshipManagementClient",
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private static string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }
    }
}