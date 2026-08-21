
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using InternshipManagement.Application.Interfaces;
using InternshipManagement.Infrastructure.Persistence;

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

        public IdentityService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole<int>> roleManager,
            IConfiguration configuration,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _configuration = configuration;
            _context = context;
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

            var result = await _signInManager.CheckPasswordSignInAsync(user, password, true);

            if (result.Succeeded)
                return SignInResult.Success();
            if (result.IsLockedOut)
                return SignInResult.LockedOut();
            if (result.IsNotAllowed)
                return SignInResult.NotAllowed();

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

        public async Task<string> GenerateJwtTokenAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return null;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.GivenName, $"{user.FirstName} {user.LastName}"),
                new Claim("userId", user.Id.ToString()),
                new Claim("userType", user.UserType)
            };

            var roles = await _userManager.GetRolesAsync(user);
            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

            var jwtSettings = _configuration.GetSection("JwtSettings");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                jwtSettings["Key"] ?? "SuperSecretKeyHere1234567890!@#$%"));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.AddHours(
                double.Parse(jwtSettings["ExpiryHours"] ?? "2"));

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
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

        public async Task<bool> UpdateRefreshTokenAsync(string userId, string refreshToken, DateTime? expiryTime)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = expiryTime;

            var result = await _userManager.UpdateAsync(user);
            return result.Succeeded;
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

        private IdentityUserDto MapToDto(ApplicationUser user)
        {
            return new IdentityUserDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                UserType = user.UserType,
                PhoneNumber = user.PhoneNumber,
                IsActive = user.IsActive,
                EmailConfirmed = user.EmailConfirmed,
                RefreshToken = user.RefreshToken,
                RefreshTokenExpiryTime = user.RefreshTokenExpiryTime
            };
        }
    }
}