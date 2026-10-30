using InternshipManagement.Application.DTOs.Student;
using System.Threading.Tasks;

namespace InternshipManagement.Application.Interfaces
{
    public interface IStudentService
    {
        Task<StudentProfileResponse> GetStudentProfileAsync(int userId);
        Task<StudentProfileResponse> UpdateStudentProfileAsync(int userId, UpdateStudentProfileRequest request);
        Task<bool> UploadResumeAsync(int userId, Guid resumeDocumentId);

        // Education
        Task<bool> AddEducationAsync(int userId, AddEducationRequest request);
        Task<bool> UpdateEducationAsync(int userId, int educationId, UpdateEducationRequest request);
        Task<bool> DeleteEducationAsync(int userId, int educationId);

        // Work Experience
        Task<bool> AddWorkExperienceAsync(int userId, AddWorkExperienceRequest request);
        Task<bool> UpdateWorkExperienceAsync(int userId, int experienceId, UpdateWorkExperienceRequest request);
        Task<bool> DeleteWorkExperienceAsync(int userId, int experienceId);

        // Skills
        Task<bool> AddSkillAsync(int userId, AddSkillRequest request);
        Task<bool> DeleteSkillAsync(int userId, int skillId);

        // Certifications
        Task<bool> AddCertificationAsync(int userId, AddCertificationRequest request);
        Task<bool> UpdateCertificationAsync(int userId, int certificationId, UpdateCertificationRequest request);
        Task<bool> DeleteCertificationAsync(int userId, int certificationId);

        // Projects
        Task<bool> AddProjectAsync(int userId, AddProjectRequest request);
        Task<bool> UpdateProjectAsync(int userId, int projectId, UpdateProjectRequest request);
        Task<bool> DeleteProjectAsync(int userId, int projectId);
    }
}
