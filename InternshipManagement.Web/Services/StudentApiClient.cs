using System.Net.Http.Headers;
using System.Net.Http.Json;
using InternshipManagement.Web.Models.Student;

namespace InternshipManagement.Web.Services
{
    public class StudentApiClient : IStudentApiClient
    {
        private readonly HttpClient _httpClient;

        public StudentApiClient(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("InternshipApi");
        }

        private void AddAuthorization(string token)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        public async Task<StudentProfileResponse?> GetProfileAsync(string token, int userId)
        {
            AddAuthorization(token);
            var response = await _httpClient.GetAsync($"api/Student/profile?userId={userId}");

            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<StudentProfileResponse>();

            return null;
        }

        public async Task<StudentProfileResponse?> UpdateProfileAsync(string token, int userId, UpdateStudentProfileRequest request)
        {
            AddAuthorization(token);
            var response = await _httpClient.PutAsJsonAsync($"api/Student/profile?userId={userId}", request);

            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<StudentProfileResponse>();

            return null;
        }

        public async Task<bool> UploadResumeAsync(string token, int userId, string resumeUrl)
        {
            AddAuthorization(token);
            var response = await _httpClient.PostAsJsonAsync($"api/Student/resume?userId={userId}", new { ResumeUrl = resumeUrl });
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> AddEducationAsync(string token, int userId, AddEducationRequest request)
        {
            AddAuthorization(token);
            var response = await _httpClient.PostAsJsonAsync($"api/Student/education?userId={userId}", request);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> AddWorkExperienceAsync(string token, int userId, AddWorkExperienceRequest request)
        {
            AddAuthorization(token);
            var response = await _httpClient.PostAsJsonAsync($"api/Student/experience?userId={userId}", request);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> AddSkillAsync(string token, int userId, AddSkillRequest request)
        {
            AddAuthorization(token);
            var response = await _httpClient.PostAsJsonAsync($"api/Student/skill?userId={userId}", request);
            return response.IsSuccessStatusCode;
        }
    }
}