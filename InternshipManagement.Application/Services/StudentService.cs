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
                .Include(s => s.Certifications)
                .Include(s => s.Projects)
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

            if (request.ProfilePictureUrl != null
                && !request.ProfilePictureUrl.StartsWith("/uploads/profile-pictures/", StringComparison.Ordinal))
                throw new InvalidOperationException("Profile pictures must be uploaded as image files.");

            var requestedDocumentIds = new[]
            {
                request.ResumeDocumentId,
                request.CoverLetterDocumentId,
                request.AcademicTranscriptDocumentId,
                request.QualificationDocumentId,
                request.IdentificationDocumentId,
                request.CertificatesDocumentId,
                request.OtherSupportingDocumentsId
            }
            .Where(value => value.HasValue)
            .Select(value => value!.Value)
            .Distinct()
            .ToList();
            var ownedDocumentCount = await _context.PrivateDocuments.CountAsync(document =>
                requestedDocumentIds.Contains(document.Id)
                && document.OwnerUserId == userId
                && document.RetentionState == InternshipManagement.Domain.Enums.DocumentRetentionState.Active);
            if (ownedDocumentCount != requestedDocumentIds.Count)
                throw new InvalidOperationException("Profile documents must be active files uploaded by this student.");

            profile.Bio = request.Bio ?? profile.Bio;
            profile.Location = request.Location ?? profile.Location;
            profile.PhoneNumber = request.PhoneNumber ?? profile.PhoneNumber;
            profile.LinkedInUrl = request.LinkedInUrl ?? profile.LinkedInUrl;
            profile.PortfolioUrl = request.PortfolioUrl ?? profile.PortfolioUrl;
            profile.University = request.University ?? profile.University;
            profile.Programme = request.Programme ?? profile.Programme;
            profile.YearOfStudy = request.YearOfStudy ?? profile.YearOfStudy;
            profile.ExpectedGraduationYear = request.ExpectedGraduationYear ?? profile.ExpectedGraduationYear;
            profile.ResumeDocumentId = request.ResumeDocumentId ?? profile.ResumeDocumentId;
            profile.CoverLetterDocumentId = request.CoverLetterDocumentId ?? profile.CoverLetterDocumentId;
            profile.AcademicTranscriptDocumentId = request.AcademicTranscriptDocumentId ?? profile.AcademicTranscriptDocumentId;
            profile.QualificationDocumentId = request.QualificationDocumentId ?? profile.QualificationDocumentId;
            profile.IdentificationDocumentId = request.IdentificationDocumentId ?? profile.IdentificationDocumentId;
            profile.CertificatesDocumentId = request.CertificatesDocumentId ?? profile.CertificatesDocumentId;
            profile.OtherSupportingDocumentsId = request.OtherSupportingDocumentsId ?? profile.OtherSupportingDocumentsId;
            profile.ProfilePictureUrl = request.ProfilePictureUrl ?? profile.ProfilePictureUrl;
            profile.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return await GetStudentProfileAsync(userId);
        }

        public async Task<bool> UploadResumeAsync(int userId, Guid resumeDocumentId)
        {
            var isOwnedActiveDocument = await _context.PrivateDocuments.AnyAsync(document =>
                document.Id == resumeDocumentId
                && document.OwnerUserId == userId
                && document.Category == "Resume"
                && document.RetentionState == InternshipManagement.Domain.Enums.DocumentRetentionState.Active);
            if (!isOwnedActiveDocument)
                throw new InvalidOperationException("The resume must be an active file uploaded by this student.");

            var profile = await _context.StudentProfiles
                .FirstOrDefaultAsync(s => s.UserId == userId && !s.IsDeleted);

            if (profile == null) return false;

            profile.ResumeDocumentId = resumeDocumentId;
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
                Grade = request.Grade,
                CompletionStatus = request.CompletionStatus,
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
            education.CompletionStatus = request.CompletionStatus;
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

        public async Task<bool> AddCertificationAsync(int userId, AddCertificationRequest request)
        {
            var profile = await _context.StudentProfiles
                .FirstOrDefaultAsync(s => s.UserId == userId && !s.IsDeleted);

            if (profile == null) return false;

            var certification = new StudentCertification
            {
                StudentProfileId = profile.Id,
                Name = request.Name,
                IssuingOrganization = request.IssuingOrganization,
                IssueDate = request.IssueDate,
                ExpirationDate = request.ExpirationDate
            };

            _context.StudentCertifications.Add(certification);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> UpdateCertificationAsync(int userId, int certificationId, UpdateCertificationRequest request)
        {
            var profile = await _context.StudentProfiles
                .FirstOrDefaultAsync(s => s.UserId == userId && !s.IsDeleted);

            if (profile == null) return false;

            var certification = await _context.StudentCertifications
                .FirstOrDefaultAsync(c => c.Id == certificationId && c.StudentProfileId == profile.Id && !c.IsDeleted);

            if (certification == null) return false;

            certification.Name = request.Name;
            certification.IssuingOrganization = request.IssuingOrganization;
            certification.IssueDate = request.IssueDate;
            certification.ExpirationDate = request.ExpirationDate;
            certification.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteCertificationAsync(int userId, int certificationId)
        {
            var profile = await _context.StudentProfiles
                .FirstOrDefaultAsync(s => s.UserId == userId && !s.IsDeleted);

            if (profile == null) return false;

            var certification = await _context.StudentCertifications
                .FirstOrDefaultAsync(c => c.Id == certificationId && c.StudentProfileId == profile.Id && !c.IsDeleted);

            if (certification == null) return false;

            certification.IsDeleted = true;
            certification.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> AddProjectAsync(int userId, AddProjectRequest request)
        {
            var profile = await _context.StudentProfiles
                .FirstOrDefaultAsync(s => s.UserId == userId && !s.IsDeleted);

            if (profile == null) return false;

            var project = new StudentProject
            {
                StudentProfileId = profile.Id,
                Title = request.Title,
                Description = request.Description,
                Role = request.Role,
                Technologies = request.Technologies,
                StartDate = request.StartDate,
                EndDate = request.EndDate
            };

            _context.StudentProjects.Add(project);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> UpdateProjectAsync(int userId, int projectId, UpdateProjectRequest request)
        {
            var profile = await _context.StudentProfiles
                .FirstOrDefaultAsync(s => s.UserId == userId && !s.IsDeleted);

            if (profile == null) return false;

            var project = await _context.StudentProjects
                .FirstOrDefaultAsync(p => p.Id == projectId && p.StudentProfileId == profile.Id && !p.IsDeleted);

            if (project == null) return false;

            project.Title = request.Title;
            project.Description = request.Description;
            project.Role = request.Role;
            project.Technologies = request.Technologies;
            project.StartDate = request.StartDate;
            project.EndDate = request.EndDate;
            project.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteProjectAsync(int userId, int projectId)
        {
            var profile = await _context.StudentProfiles
                .FirstOrDefaultAsync(s => s.UserId == userId && !s.IsDeleted);

            if (profile == null) return false;

            var project = await _context.StudentProjects
                .FirstOrDefaultAsync(p => p.Id == projectId && p.StudentProfileId == profile.Id && !p.IsDeleted);

            if (project == null) return false;

            project.IsDeleted = true;
            project.UpdatedAt = DateTime.UtcNow;

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
                ResumeDocumentId = profile.ResumeDocumentId,
                CoverLetterDocumentId = profile.CoverLetterDocumentId,
                AcademicTranscriptDocumentId = profile.AcademicTranscriptDocumentId,
                QualificationDocumentId = profile.QualificationDocumentId,
                IdentificationDocumentId = profile.IdentificationDocumentId,
                CertificatesDocumentId = profile.CertificatesDocumentId,
                OtherSupportingDocumentsId = profile.OtherSupportingDocumentsId,
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
                    Grade = e.Grade,
                    CompletionStatus = e.CompletionStatus,
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
                }).ToList() ?? new List<SkillDto>(),
                Certifications = profile.Certifications?.Select(c => new StudentCertificationDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    IssuingOrganization = c.IssuingOrganization,
                    IssueDate = c.IssueDate,
                    ExpirationDate = c.ExpirationDate,
                }).ToList() ?? new List<StudentCertificationDto>(),
                Projects = profile.Projects?.Select(p => new StudentProjectDto
                {
                    Id = p.Id,
                    Title = p.Title,
                    Description = p.Description,
                    Role = p.Role,
                    Technologies = p.Technologies,
                    StartDate = p.StartDate,
                    EndDate = p.EndDate
                }).ToList() ?? new List<StudentProjectDto>()
            };
        }

    }
}
