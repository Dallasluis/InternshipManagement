using Microsoft.EntityFrameworkCore;
using InternshipManagement.Application.DTOs.Company;
using InternshipManagement.Application.Interfaces;
using InternshipManagement.Domain.Entities;
using InternshipManagement.Domain.Enums;

namespace InternshipManagement.Application.Services
{
    public class CompanyService : ICompanyService
    {
        private readonly IApplicationDbContext _context;

        public CompanyService(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<CompanyProfileResponse> GetCompanyProfileAsync(int userId)
        {
            var profile = await _context.CompanyProfiles
                .Include(c => c.Internships)
                .FirstOrDefaultAsync(c => c.UserId == userId && !c.IsDeleted);

            if (profile == null) return null;

            return await MapToResponse(profile);
        }

        public async Task<CompanyProfileResponse> UpdateCompanyProfileAsync(int userId, UpdateCompanyProfileRequest request)
        {
            var profile = await _context.CompanyProfiles
                .FirstOrDefaultAsync(c => c.UserId == userId && !c.IsDeleted);

            if (profile == null)
                throw new Exception("Company profile not found.");

            profile.Description = request.Description ?? profile.Description;
            profile.Industry = request.Industry ?? profile.Industry;
            profile.Website = request.Website ?? profile.Website;
            profile.LinkedInUrl = request.LinkedInUrl ?? profile.LinkedInUrl;
            profile.Address = request.Address ?? profile.Address;
            profile.City = request.City ?? profile.City;
            profile.Country = request.Country ?? profile.Country;
            profile.PhoneNumber = request.PhoneNumber ?? profile.PhoneNumber;
            profile.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return await MapToResponse(profile);
        }

        public async Task<bool> SubmitVerificationAsync(int userId, SubmitVerificationRequest request)
        {
            var profile = await _context.CompanyProfiles
                .FirstOrDefaultAsync(c => c.UserId == userId && !c.IsDeleted);

            if (profile == null) return false;

            profile.VerificationDocuments = request.VerificationDocuments;
            profile.AdminNotes = request.Notes;
            profile.VerificationStatus = CompanyVerificationStatus.UnderReview;
            profile.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ReviewVerificationAsync(int companyProfileId, ReviewVerificationRequest request)
        {
            var profile = await _context.CompanyProfiles
                .FirstOrDefaultAsync(c => c.Id == companyProfileId && !c.IsDeleted);

            if (profile == null) return false;

            if (request.Approved)
            {
                profile.VerificationStatus = CompanyVerificationStatus.Verified;
                profile.VerifiedAt = DateTime.UtcNow;
                profile.IsSubscribed = true;
                profile.SubscriptionStartDate = DateTime.UtcNow;
                profile.SubscriptionEndDate = DateTime.UtcNow.AddMonths(1);
            }
            else
            {
                profile.VerificationStatus = CompanyVerificationStatus.Rejected;
                profile.AdminNotes = request.Notes;
            }

            profile.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateSubscriptionStatusAsync(int companyProfileId, bool isSubscribed)
        {
            var profile = await _context.CompanyProfiles
                .FirstOrDefaultAsync(c => c.Id == companyProfileId && !c.IsDeleted);

            if (profile == null) return false;

            profile.IsSubscribed = isSubscribed;
            profile.UpdatedAt = DateTime.UtcNow;

            if (isSubscribed)
            {
                profile.SubscriptionStartDate = DateTime.UtcNow;
                profile.SubscriptionEndDate = DateTime.UtcNow.AddMonths(1);
            }
            else
            {
                profile.SubscriptionStartDate = null;
                profile.SubscriptionEndDate = null;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        private async Task<CompanyProfileResponse> MapToResponse(CompanyProfile profile)
        {
            var activeInternships = profile.Internships?
                .Count(i => i.Status == InternshipStatus.Published && !i.IsDeleted) ?? 0;

            var totalInternships = profile.Internships?
                .Count(i => !i.IsDeleted) ?? 0;

            var totalApplications = await _context.InternshipApplications
                .CountAsync(a => a.Internship.CompanyProfileId == profile.Id && !a.IsDeleted);

            return new CompanyProfileResponse
            {
                Id = profile.Id,
                CompanyName = profile.CompanyName,
                Description = profile.Description,
                Industry = profile.Industry,
                Website = profile.Website,
                LinkedInUrl = profile.LinkedInUrl,
                LogoUrl = profile.LogoUrl,
                Address = profile.Address,
                City = profile.City,
                Country = profile.Country,
                PhoneNumber = profile.PhoneNumber,
                VerificationStatus = profile.VerificationStatus.ToString(),
                IsSubscribed = profile.IsSubscribed,
                SubscriptionStartDate = profile.SubscriptionStartDate,
                SubscriptionEndDate = profile.SubscriptionEndDate,
                ActiveInternships = activeInternships,
                TotalInternships = totalInternships,
                TotalApplications = totalApplications
            };
        }
    }
}