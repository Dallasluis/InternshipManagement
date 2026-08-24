using InternshipManagement.Web.Models.Student;

namespace InternshipManagement.Web.Services
{
    public interface IStudentApiClient
    {
        Task<StudentProfileResponse?> GetProfileAsync(string token, int userId);
        Task<StudentProfileResponse?> UpdateProfileAsync(string token, int userId, UpdateStudentProfileRequest request);
        Task<bool> UploadResumeAsync(string token, int userId, string resumeUrl);
        Task<bool> AddEducationAsync(string token, int userId, AddEducationRequest request);
        Task<bool> AddWorkExperienceAsync(string token, int userId, AddWorkExperienceRequest request);
        Task<bool> AddSkillAsync(string token, int userId, AddSkillRequest request);
    }
}