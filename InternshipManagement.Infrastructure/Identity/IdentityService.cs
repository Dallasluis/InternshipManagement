
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using InternshipManagement.Application.DTOs.Auth;
using InternshipManagement.Application.Interfaces;
using InternshipManagement.Infrastructure.Persistence;
using InternshipManagement.Application.Services;

using IdentityResult = InternshipManagement.Application.Interfaces.IdentityResult;
using SignInResult = InternshipManagement.Application.Interfaces.SignInResult;

namespace InternshipManagement.Infrastructure.Identity
{
    public class IdentityService : IIdentityService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole<int>> _roleManager;
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;
        private readonly LoginSecurityOptions _loginSecurityOptions;
        private readonly JwtConfiguration _jwtConfiguration;

        public IdentityService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole<int>> roleManager,
            IConfiguration configuration,
            ApplicationDbContext context,
            IOptions<LoginSecurityOptions> loginSecurityOptions)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _configuration = configuration;
            _context = context;
            _loginSecurityOptions = loginSecurityOptions.Value;
            _jwtConfiguration = JwtConfiguration.From(configuration);
        }

        public async Task<IdentityResult> CreateUserAsync(string email, string password, string firstName, string lastName, string userType)
        {
            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                UserType = userType,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, password);

            if (!result.Succeeded)
            {
                return IdentityResult.Failure(result.Errors.Select(e => e.Description).ToList());
            }

            return IdentityResult.Success(user.Id.ToString());
        }

        public async Task<IdentityUserDto> FindUserByEmailAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return null;

            return MapToDto(user);
        }

        public async Task<IdentityUserDto> FindUserByIdAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return null;

            return MapToDto(user);
        }

        public async Task<bool> CheckPasswordAsync(string email, string password)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return false;

            return await _userManager.CheckPasswordAsync(user, password);
        }

        public async Task<SignInResult> SignInAsync(string email, string password, bool isPersistent)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return SignInResult.Failed();
            }

            var now = DateTimeOffset.UtcNow;
            if (user.LockoutEnd > now)
                return SignInResult.LockedOut();

            var result = await _signInManager.CheckPasswordSignInAsync(user, password, false);

            if (result.Succeeded)
            {
                user.FailedLoginAttempts = 0;
                user.FailedLoginWindowStartedAt = null;
                user.LockoutEscalationLevel = 0;
                user.LastLoginAt = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);
                return SignInResult.Success();
            }
            if (result.IsNotAllowed)
                return SignInResult.NotAllowed();

            if (user.FailedLoginWindowStartedAt == null
                || user.FailedLoginWindowStartedAt < now.AddMinutes(-_loginSecurityOptions.LockoutWindowMinutes))
            {
                user.FailedLoginAttempts = 0;
                user.FailedLoginWindowStartedAt = now;
            }

            user.FailedLoginAttempts++;
            if (user.FailedLoginAttempts >= _loginSecurityOptions.LockoutMaxFailedAttempts)
            {
                var lockoutMinutes = Math.Min(
                    _loginSecurityOptions.InitialLockoutMinutes * Math.Pow(2, user.LockoutEscalationLevel),
                    _loginSecurityOptions.MaxLockoutMinutes);
                user.LockoutEscalationLevel = Math.Min(user.LockoutEscalationLevel + 1, 10);
                user.FailedLoginAttempts = 0;
                user.FailedLoginWindowStartedAt = null;
                await _userManager.SetLockoutEndDateAsync(user, now.AddMinutes(lockoutMinutes));
                await _userManager.UpdateAsync(user);
                return SignInResult.LockedOut(newlyLockedOut: true);
            }

            await _userManager.UpdateAsync(user);
            return SignInResult.Failed();
        }

        public async Task SignOutAsync()
        {
            await _signInManager.SignOutAsync();
        }

        public async Task<bool> IsInRoleAsync(string userId, string role)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            return await _userManager.IsInRoleAsync(user, role);
        }

        public async Task<IList<string>> GetUserRolesAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return new List<string>();

            return await _userManager.GetRolesAsync(user);
        }

        public async Task<IdentityResult> AddToRoleAsync(string userId, string role)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return IdentityResult.Failure(new List<string> { "User not found" });
            }

            var result = await _userManager.AddToRoleAsync(user, role);
            if (!result.Succeeded)
            {
                return IdentityResult.Failure(result.Errors.Select(e => e.Description).ToList());
            }

            return IdentityResult.Success(userId);
        }

        public async Task<bool> RoleExistsAsync(string role)
        {
            return await _roleManager.RoleExistsAsync(role);
        }

        public async Task<IdentityResult> CreateRoleAsync(string role)
        {
            var result = await _roleManager.CreateAsync(new IdentityRole<int>(role));
            if (!result.Succeeded)
            {
                return IdentityResult.Failure(result.Errors.Select(e => e.Description).ToList());
            }
            return IdentityResult.Success();
        }

        public async Task<string> GenerateEmailConfirmationTokenAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return null;

            return await _userManager.GenerateEmailConfirmationTokenAsync(user);
        }

        public async Task<IdentityResult> ConfirmEmailAsync(string userId, string token)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return IdentityResult.Failure(new List<string> { "User not found" });
            }

            var result = await _userManager.ConfirmEmailAsync(user, token);
            if (!result.Succeeded)
            {
                return IdentityResult.Failure(result.Errors.Select(e => e.Description).ToList());
            }

            return IdentityResult.Success(userId);
        }

        public async Task<string> GeneratePasswordResetTokenAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return null;

            return await _userManager.GeneratePasswordResetTokenAsync(user);
        }

        public async Task<IdentityResult> ResetPasswordAsync(string userId, string token, string newPassword)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return IdentityResult.Failure(new List<string> { "User not found" });
            }

            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
            if (!result.Succeeded)
            {
                return IdentityResult.Failure(result.Errors.Select(e => e.Description).ToList());
            }

            return IdentityResult.Success(userId);
        }

        public async Task<string> GenerateJwtTokenAsync(
            string userId,
            bool rememberMe = false,
            bool twoFactorSetupOnly = false,
            DateTime? absoluteSessionExpiresAt = null,
            bool twoFactorLoginOnly = false,
            string? loginChallengeId = null)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return null;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.GivenName, $"{user.FirstName} {user.LastName}"),
                new Claim("userId", user.Id.ToString()),
                new Claim("userType", user.UserType),
                new Claim("email_verified", user.EmailConfirmed ? "true" : "false")
            };
            if (twoFactorSetupOnly)
                claims.Add(new Claim("two_factor_setup", "true"));
            if (twoFactorLoginOnly && !string.IsNullOrWhiteSpace(loginChallengeId))
            {
                claims.Add(new Claim("two_factor_login", "true"));
                claims.Add(new Claim("login_challenge_id", loginChallengeId));
                claims.Add(new Claim("login_remember_me", rememberMe ? "true" : "false"));
            }

            var roles = await _userManager.GetRolesAsync(user);
            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

            var key = new SymmetricSecurityKey(_jwtConfiguration.SigningKey);

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var now = DateTime.UtcNow;
            var duration = user.UserType.Equals("Student", StringComparison.OrdinalIgnoreCase)
                ? TimeSpan.FromHours(_loginSecurityOptions.StudentIdleTimeoutHours)
                : TimeSpan.FromMinutes(_loginSecurityOptions.AdminCompanyIdleTimeoutMinutes);
            var expires = twoFactorSetupOnly || twoFactorLoginOnly ? now.AddMinutes(10) : now.Add(duration);
            var absoluteExpiry = absoluteSessionExpiresAt
                ?? (rememberMe && user.UserType.Equals("Student", StringComparison.OrdinalIgnoreCase)
                    ? now.AddDays(_loginSecurityOptions.RememberStudentDays)
                    : now.AddHours(_loginSecurityOptions.AbsoluteSessionHours));
            if (expires > absoluteExpiry)
                expires = absoluteExpiry;

            var token = new JwtSecurityToken(
                issuer: _jwtConfiguration.Issuer,
                audience: _jwtConfiguration.Audience,
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        public async Task<bool> UpdateRefreshTokenAsync(
            string userId,
            string? refreshToken,
            DateTime? expiryTime,
            bool rememberMe = false,
            DateTime? absoluteSessionExpiresAt = null)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            user.RefreshToken = refreshToken == null
                ? null
                : Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));
            user.RefreshTokenExpiryTime = expiryTime;
            user.RememberMe = refreshToken != null && rememberMe
                && user.UserType.Equals("Student", StringComparison.OrdinalIgnoreCase);
            user.AbsoluteSessionExpiresAt = refreshToken == null ? null : absoluteSessionExpiresAt;
            user.LastSessionActivityAt = refreshToken == null ? null : DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(user);
            return result.Succeeded;
        }

        public async Task<bool> TouchSessionAsync(string userId, DateTime activityAt)
        {
            if (!int.TryParse(userId, out var parsedUserId))
                return false;

            var user = await _context.Users
                .AsNoTracking()
                .Where(candidate => candidate.Id == parsedUserId && candidate.IsActive)
                .Select(candidate => new
                {
                    candidate.UserType,
                    candidate.AbsoluteSessionExpiresAt,
                    candidate.RefreshTokenExpiryTime
                })
                .FirstOrDefaultAsync();
            if (user?.AbsoluteSessionExpiresAt is not DateTime absoluteExpiry
                || absoluteExpiry <= activityAt
                || user.RefreshTokenExpiryTime is not DateTime refreshExpiry
                || refreshExpiry <= activityAt)
            {
                return false;
            }

            var idleDuration = user.UserType.Equals("Student", StringComparison.OrdinalIgnoreCase)
                ? TimeSpan.FromHours(_loginSecurityOptions.StudentIdleTimeoutHours)
                : TimeSpan.FromMinutes(_loginSecurityOptions.AdminCompanyIdleTimeoutMinutes);
            var idleExpiry = activityAt.Add(idleDuration);

            var updatedRows = await _context.Users
                .Where(candidate => candidate.Id == parsedUserId
                    && candidate.IsActive
                    && candidate.AbsoluteSessionExpiresAt > activityAt
                    && candidate.RefreshTokenExpiryTime > activityAt)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(candidate => candidate.LastSessionActivityAt, activityAt)
                    .SetProperty(
                        candidate => candidate.RefreshTokenExpiryTime,
                        candidate => candidate.AbsoluteSessionExpiresAt!.Value <= idleExpiry
                            ? candidate.AbsoluteSessionExpiresAt
                            : idleExpiry));
            return updatedRows == 1;
        }

        public async Task<bool> RotateRefreshTokenAsync(
            string userId,
            string currentRefreshToken,
            string nextRefreshToken,
            DateTime expiryTime,
            DateTime absoluteSessionExpiresAt,
            bool rememberMe)
        {
            if (!int.TryParse(userId, out var parsedUserId))
                return false;

            var now = DateTime.UtcNow;
            var currentTokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(currentRefreshToken)));
            var nextTokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(nextRefreshToken)));
            var updatedRows = await _context.Users
                .Where(candidate => candidate.Id == parsedUserId
                    && candidate.RefreshToken == currentTokenHash
                    && candidate.IsActive
                    && candidate.RefreshTokenExpiryTime > now
                    && candidate.AbsoluteSessionExpiresAt == absoluteSessionExpiresAt
                    && candidate.AbsoluteSessionExpiresAt > now)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(candidate => candidate.RefreshToken, nextTokenHash)
                    .SetProperty(candidate => candidate.RefreshTokenExpiryTime, expiryTime)
                    .SetProperty(candidate => candidate.LastSessionActivityAt, now)
                    .SetProperty(
                        candidate => candidate.RememberMe,
                        candidate => rememberMe && candidate.UserType == "Student"));
            return updatedRows == 1;
        }

        public async Task<IdentityUserDto?> FindUserByRefreshTokenAsync(string refreshToken)
        {
            var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));
            var user = await _userManager.Users.FirstOrDefaultAsync(candidate => candidate.RefreshToken == tokenHash);
            return user == null ? null : MapToDto(user);
        }

        public async Task<ClaimsPrincipal> GetUserPrincipalAsync()
        {
            return await _signInManager.CreateUserPrincipalAsync(
                await _userManager.FindByIdAsync(_signInManager.Context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value)
            );
        }

        public async Task<IdentityUserDto> GetCurrentUserAsync()
        {
            var userId = _signInManager.Context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return null;

            return await FindUserByIdAsync(userId);
        }

        public async Task<List<IdentityUserDto>> GetAllUsersAsync()
        {
            return await _userManager.Users
                .OrderByDescending(u => u.CreatedAt)
                .Select(u => new IdentityUserDto
                {
                    Id = u.Id,
                    Email = u.Email,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    UserType = u.UserType,
                    PhoneNumber = u.PhoneNumber,
                    IsActive = u.IsActive,
                    EmailConfirmed = u.EmailConfirmed,
                    CreatedAt = u.CreatedAt,
                    LastLoginAt = u.LastLoginAt
                })
                .ToListAsync();
        }

        public async Task<bool> SetUserActiveStatusAsync(string userId, bool isActive)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            user.IsActive = isActive;
            if (!isActive)
            {
                user.RefreshToken = null;
                user.RefreshTokenExpiryTime = null;
                user.AbsoluteSessionExpiresAt = null;
                user.LastSessionActivityAt = null;
            }
            var result = await _userManager.UpdateAsync(user);
            return result.Succeeded;
        }

        public async Task<IdentityResult> ChangePasswordAsync(string userId, string currentPassword, string newPassword)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return IdentityResult.Failure(new List<string> { "User not found" });

            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
            return result.Succeeded
                ? IdentityResult.Success(userId)
                : IdentityResult.Failure(result.Errors.Select(e => e.Description).ToList());
        }

        public async Task<IdentityResult> ChangeEmailAsync(string userId, string currentPassword, string newEmail)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return IdentityResult.Failure(new List<string> { "User not found" });
            if (!await _userManager.CheckPasswordAsync(user, currentPassword))
                return IdentityResult.Failure(new List<string> { "Current password is incorrect" });

            var emailResult = await _userManager.SetEmailAsync(user, newEmail);
            if (!emailResult.Succeeded)
                return IdentityResult.Failure(emailResult.Errors.Select(e => e.Description).ToList());

            var nameResult = await _userManager.SetUserNameAsync(user, newEmail);
            return nameResult.Succeeded
                ? IdentityResult.Success(userId)
                : IdentityResult.Failure(nameResult.Errors.Select(e => e.Description).ToList());
        }

        public async Task<IdentityResult> SetEmailAfterConfirmationAsync(string userId, string newEmail)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return IdentityResult.Failure(new List<string> { "User not found." });

            var emailResult = await _userManager.SetEmailAsync(user, newEmail);
            if (!emailResult.Succeeded)
                return IdentityResult.Failure(emailResult.Errors.Select(e => e.Description).ToList());

            var nameResult = await _userManager.SetUserNameAsync(user, newEmail);
            if (!nameResult.Succeeded)
                return IdentityResult.Failure(nameResult.Errors.Select(e => e.Description).ToList());

            user.EmailConfirmed = true;
            var updateResult = await _userManager.UpdateAsync(user);
            return updateResult.Succeeded
                ? IdentityResult.Success(userId)
                : IdentityResult.Failure(updateResult.Errors.Select(e => e.Description).ToList());
        }

        public async Task<IdentityResult> ConfirmEmailAddressAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return IdentityResult.Failure(new List<string> { "User not found." });

            user.EmailConfirmed = true;
            var result = await _userManager.UpdateAsync(user);
            return result.Succeeded
                ? IdentityResult.Success(userId)
                : IdentityResult.Failure(result.Errors.Select(e => e.Description).ToList());
        }

        public async Task<IdentityResult> DeleteUserAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return IdentityResult.Failure(new List<string> { "User not found." });

            var result = await _userManager.DeleteAsync(user);
            return result.Succeeded
                ? IdentityResult.Success(userId)
                : IdentityResult.Failure(result.Errors.Select(e => e.Description).ToList());
        }

        public async Task<string?> GetUserTokenAsync(string userId, string name)
        {
            var user = await _userManager.FindByIdAsync(userId);
            return user == null ? null : await _userManager.GetAuthenticationTokenAsync(user, "InternshipManagement", name);
        }

        public async Task SetUserTokenAsync(string userId, string name, string value)
        {
            var user = await _userManager.FindByIdAsync(userId)
                ?? throw new InvalidOperationException("User not found.");
            var result = await _userManager.SetAuthenticationTokenAsync(user, "InternshipManagement", name, value);
            if (!result.Succeeded)
                throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
        }

        public async Task RemoveUserTokenAsync(string userId, string name)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
                await _userManager.RemoveAuthenticationTokenAsync(user, "InternshipManagement", name);
        }

        public async Task<IdentityResult> UpdateProfilePictureUrlAsync(string userId, string profilePictureUrl)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return IdentityResult.Failure(new List<string> { "User not found." });

            user.ProfilePictureUrl = profilePictureUrl;
            var result = await _userManager.UpdateAsync(user);
            return result.Succeeded
                ? IdentityResult.Success(userId)
                : IdentityResult.Failure(result.Errors.Select(e => e.Description).ToList());
        }

        public async Task<AccountPreferencesDto> GetAccountPreferencesAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || string.IsNullOrWhiteSpace(user.AccountPreferencesJson))
                return new AccountPreferencesDto();

            return JsonSerializer.Deserialize<AccountPreferencesDto>(user.AccountPreferencesJson) ?? new AccountPreferencesDto();
        }

        public async Task<IdentityResult> UpdateAccountPreferencesAsync(string userId, AccountPreferencesDto preferences)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return IdentityResult.Failure(new List<string> { "User not found" });

            user.AccountPreferencesJson = JsonSerializer.Serialize(preferences);
            var result = await _userManager.UpdateAsync(user);
            return result.Succeeded
                ? IdentityResult.Success(userId)
                : IdentityResult.Failure(result.Errors.Select(e => e.Description).ToList());
        }

        public async Task<IdentityResult> DeactivateAccountAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return IdentityResult.Failure(new List<string> { "User not found" });

            user.IsActive = false;
            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = DateTime.UtcNow;
            var result = await _userManager.UpdateAsync(user);
            return result.Succeeded
                ? IdentityResult.Success(userId)
                : IdentityResult.Failure(result.Errors.Select(e => e.Description).ToList());
        }

        private IdentityUserDto MapToDto(ApplicationUser user)
        {
            return new IdentityUserDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                UserType = user.UserType,
                ProfilePictureUrl = user.ProfilePictureUrl,
                TwoFactorEnabled = user.TwoFactorEnabled,
                RememberMe = user.RememberMe,
                LockoutEscalationLevel = user.LockoutEscalationLevel,
                PhoneNumber = user.PhoneNumber,
                IsActive = user.IsActive,
                EmailConfirmed = user.EmailConfirmed,
                RefreshToken = user.RefreshToken,
                RefreshTokenExpiryTime = user.RefreshTokenExpiryTime,
                AbsoluteSessionExpiresAt = user.AbsoluteSessionExpiresAt,
                LastSessionActivityAt = user.LastSessionActivityAt,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt
            };
        }
    }
}
