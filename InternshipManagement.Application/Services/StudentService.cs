using Microsoft.EntityFrameworkCore;
using InternshipManagement.Application.DTOs.Student;
using InternshipManagement.Application.Interfaces;
using InternshipManagement.Domain.Entities;

namespace InternshipManagement.Application.Services
{
    public class StudentService : IStudentService
    {
        private readonly IApplicationDbContext _context;

        public StudentService(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<StudentProfileResponse> GetStudentProfileAsync(int userId)
        {
            var profile = await _context.StudentProfiles
                .Include(s => s.Education)
                .Include(s => s.WorkExperience)
                .Include(s => s.Skills)
                .FirstOrDefaultAsync(s => s.UserId == userId && !s.IsDeleted);

            if (profile == null) return null;

            return MapToResponse(profile);
        }

        public async Task<StudentProfileResponse> UpdateStudentProfileAsync(int userId, UpdateStudentProfileRequest request)
        {
            var profile = await _context.StudentProfiles
                .FirstOrDefaultAsync(s => s.UserId == userId && !s.IsDeleted);

            if (profile == null)
                throw new Exception("Student profile not found.");

            profile.Bio = request.Bio ?? profile.Bio;
            profile.Location = request.Location ?? profile.Location;
            profile.PhoneNumber = request.PhoneNumber ?? profile.PhoneNumber;
            profile.LinkedInUrl = request.LinkedInUrl ?? profile.LinkedInUrl;
            profile.PortfolioUrl = request.PortfolioUrl ?? profile.PortfolioUrl;
            profile.University = request.University ?? profile.University;
            profile.Programme = request.Programme ?? profile.Programme;
            profile.YearOfStudy = request.YearOfStudy ?? profile.YearOfStudy;
            profile.ExpectedGraduationYear = request.ExpectedGraduationYear ?? profile.ExpectedGraduationYear;
            profile.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return await GetStudentProfileAsync(userId);
        }

        public async Task<bool> UploadResumeAsync(int userId, string resumeUrl)
        {
            var profile = await _context.StudentProfiles
                .FirstOrDefaultAsync(s => s.UserId == userId && !s.IsDeleted);

            if (profile == null) return false;

            profile.ResumeUrl = resumeUrl;
            profile.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> AddEducationAsync(int userId, AddEducationRequest request)
        {
            var profile = await _context.StudentProfiles
                .FirstOrDefaultAsync(s => s.UserId == userId && !s.IsDeleted);

            if (profile == null) return false;

            var education = new Education
            {
                StudentProfileId = profile.Id,
                Institution = request.Institution,
                Degree = request.Degree,
                FieldOfStudy = request.FieldOfStudy,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                IsCurrent = request.IsCurrent,
                Grade = request.Grade
            };

            _context.Educations.Add(education);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> UpdateEducationAsync(int userId, int educationId, UpdateEducationRequest request)
        {
            var profile = await _context.StudentProfiles
                .FirstOrDefaultAsync(s => s.UserId == userId && !s.IsDeleted);

            if (profile == null) return false;

            var education = await _context.Educations
                .FirstOrDefaultAsync(e => e.Id == educationId && e.StudentProfileId == profile.Id && !e.IsDeleted);

            if (education == null) return false;

            education.Institution = request.Institution;
            education.Degree = request.Degree;
            education.FieldOfStudy = request.FieldOfStudy;
            education.StartDate = request.StartDate;
            education.EndDate = request.EndDate;
            education.IsCurrent = request.IsCurrent;
            education.Grade = request.Grade;
            education.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteEducationAsync(int userId, int educationId)
        {
            var profile = await _context.StudentProfiles
                .FirstOrDefaultAsync(s => s.UserId == userId && !s.IsDeleted);

            if (profile == null) return false;

            var education = await _context.Educations
                .FirstOrDefaultAsync(e => e.Id == educationId && e.StudentProfileId == profile.Id && !e.IsDeleted);

            if (education == null) return false;

            education.IsDeleted = true;
            education.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> AddWorkExperienceAsync(int userId, AddWorkExperienceRequest request)
        {
            var profile = await _context.StudentProfiles
                .FirstOrDefaultAsync(s => s.UserId == userId && !s.IsDeleted);

            if (profile == null) return false;

            var experience = new WorkExperience
            {
                StudentProfileId = profile.Id,
                Company = request.Company,
                Position = request.Position,
                Description = request.Description,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                IsCurrent = request.IsCurrent
            };

            _context.WorkExperiences.Add(experience);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> UpdateWorkExperienceAsync(int userId, int experienceId, UpdateWorkExperienceRequest request)
        {
            var profile = await _context.StudentProfiles
                .FirstOrDefaultAsync(s => s.UserId == userId && !s.IsDeleted);

            if (profile == null) return false;

            var experience = await _context.WorkExperiences
                .FirstOrDefaultAsync(w => w.Id == experienceId && w.StudentProfileId == profile.Id && !w.IsDeleted);

            if (experience == null) return false;

            experience.Company = request.Company;
            experience.Position = request.Position;
            experience.Description = request.Description;
            experience.StartDate = request.StartDate;
            experience.EndDate = request.EndDate;
            experience.IsCurrent = request.IsCurrent;
            experience.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteWorkExperienceAsync(int userId, int experienceId)
        {
            var profile = await _context.StudentProfiles
                .FirstOrDefaultAsync(s => s.UserId == userId && !s.IsDeleted);

            if (profile == null) return false;

            var experience = await _context.WorkExperiences
                .FirstOrDefaultAsync(w => w.Id == experienceId && w.StudentProfileId == profile.Id && !w.IsDeleted);

            if (experience == null) return false;

            experience.IsDeleted = true;
            experience.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> AddSkillAsync(int userId, AddSkillRequest request)
        {
            var profile = await _context.StudentProfiles
                .FirstOrDefaultAsync(s => s.UserId == userId && !s.IsDeleted);

            if (profile == null) return false;

            var skill = new Skill
            {
                StudentProfileId = profile.Id,
                Name = request.Name,
                ProficiencyLevel = request.ProficiencyLevel
            };

            _context.Skills.Add(skill);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeleteSkillAsync(int userId, int skillId)
        {
            var profile = await _context.StudentProfiles
                .FirstOrDefaultAsync(s => s.UserId == userId && !s.IsDeleted);

            if (profile == null) return false;

            var skill = await _context.Skills
                .FirstOrDefaultAsync(s => s.Id == skillId && s.StudentProfileId == profile.Id && !s.IsDeleted);

            if (skill == null) return false;

            skill.IsDeleted = true;
            skill.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        private StudentProfileResponse MapToResponse(StudentProfile profile)
        {
            return new StudentProfileResponse
            {
                Id = profile.Id,
                FirstName = profile.FirstName,
                LastName = profile.LastName,
                Bio = profile.Bio,
                Location = profile.Location,
                PhoneNumber = profile.PhoneNumber,
                LinkedInUrl = profile.LinkedInUrl,
                PortfolioUrl = profile.PortfolioUrl,
                University = profile.University,
                Programme = profile.Programme,
                YearOfStudy = profile.YearOfStudy,
                ExpectedGraduationYear = profile.ExpectedGraduationYear,
                ResumeUrl = profile.ResumeUrl,
                ProfilePictureUrl = profile.ProfilePictureUrl,
                Education = profile.Education?.Select(e => new EducationDto
                {
                    Id = e.Id,
                    Institution = e.Institution,
                    Degree = e.Degree,
                    FieldOfStudy = e.FieldOfStudy,
                    StartDate = e.StartDate,
                    EndDate = e.EndDate,
                    IsCurrent = e.IsCurrent,
                    Grade = e.Grade
                }).ToList() ?? new List<EducationDto>(),
                WorkExperience = profile.WorkExperience?.Select(w => new WorkExperienceDto
                {
                    Id = w.Id,
                    Company = w.Company,
                    Position = w.Position,
                    Description = w.Description,
                    StartDate = w.StartDate,
                    EndDate = w.EndDate,
                    IsCurrent = w.IsCurrent
                }).ToList() ?? new List<WorkExperienceDto>(),
                Skills = profile.Skills?.Select(s => new SkillDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    ProficiencyLevel = s.ProficiencyLevel
                }).ToList() ?? new List<SkillDto>()
            };
        }
    }
}