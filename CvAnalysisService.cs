using InternshipManagement.Application.DTOs.Student;
using InternshipManagement.Application.Interfaces;
using InternshipManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace InternshipManagement.Application.Services
{
    public class CvAnalysisService : ICvAnalysisService
    {
        // Must match the Webhook node path in the n8n "CV analyze" workflow.
        private const string WorkflowPath = "cv/analyze";
        private const string RunTokenPurpose = "cv";

        private const int MaxPdfBytes = 5 * 1024 * 1024;
        private const int MaxPdfPages = 8;
        private const int MinCvChars = 200;
        private const int MaxCvChars = 15000;

        // Caps on what one CV may suggest or one confirm request may add.
        private const int MaxSkills = 40;
        private const int MaxEducation = 6;
        private const int MaxWorkExperience = 10;
        private const int MaxCertifications = 15;
        private const int MaxProjects = 8;
        private const int MaxNotes = 5;

        private static readonly string[] ProficiencyLevels = { "Beginner", "Intermediate", "Advanced" };
        private static readonly string[] CurrentWords = { "present", "current", "now", "ongoing", "to date", "today" };
        private static readonly string[] DateFormats = { "yyyy-MM", "yyyy", "yyyy-MM-dd" };

        private readonly IApplicationDbContext _context;
        private readonly IN8nClient _n8n;
        private readonly IRunTokenService _runTokens;
        private readonly IPdfTextExtractor _pdf;
        private readonly IStudentService _students;

        public CvAnalysisService(
            IApplicationDbContext context,
            IN8nClient n8n,
            IRunTokenService runTokens,
            IPdfTextExtractor pdf,
            IStudentService students)
        {
            _context = context;
            _n8n = n8n;
            _runTokens = runTokens;
            _pdf = pdf;
            _students = students;
        }

        // =====================================================================================
        // Analyze: PDF -> text -> contact details removed -> AI -> validated suggestions
        // =====================================================================================

        public async Task<CvAnalysisResponse> AnalyzeAsync(
            int userId, byte[] pdfBytes, CancellationToken cancellationToken = default)
        {
            var cvText = await PrepareAsync(userId, pdfBytes, cancellationToken);
            return await AnalyzeTextAsync(userId, cvText, cancellationToken);
        }

        public async Task<string> PrepareAsync(
            int userId, byte[] pdfBytes, CancellationToken cancellationToken = default)
        {
            var profile = await LoadProfileAsync(userId, cancellationToken);

            if (pdfBytes == null || pdfBytes.Length == 0)
                throw new InvalidOperationException("No file received.");
            if (pdfBytes.Length > MaxPdfBytes)
                throw new InvalidOperationException("The CV is larger than 5 MB.");
            if (!LooksLikePdf(pdfBytes))
                throw new InvalidOperationException("Only PDF files are supported.");

            string rawText;
            try
            {
                rawText = _pdf.ExtractText(pdfBytes, MaxPdfPages);
            }
            catch (PdfUnreadableException)
            {
                throw new InvalidOperationException(
                    "The PDF could not be read. Export it again as a normal PDF without a password.");
            }

            // Contact details and ID numbers never leave the API.
            var cvText = CvTextSanitizer.Sanitize(rawText, new[] { profile.FirstName, profile.LastName });

            if (cvText.Length < MinCvChars)
                throw new InvalidOperationException(
                    "No readable text was found in this PDF. Scanned or image-only CVs are not supported yet: export your CV as a text PDF.");

            if (cvText.Length > MaxCvChars)
                cvText = cvText[..MaxCvChars];

            return cvText;
        }

        public async Task<CvAnalysisResponse> AnalyzeTextAsync(
            int userId, string cvText, CancellationToken cancellationToken = default)
        {
            var profile = await LoadProfileAsync(userId, cancellationToken);

            var payload = new N8nCvAnalysisRequest
            {
                CvText = cvText,
                AllowedProficiencyLevels = ProficiencyLevels,
                // Lets the AI's tools call back as this student for a few minutes (read-only reference data).
                RunToken = _runTokens.Create(userId, "Student", RunTokenPurpose)
            };

            var raw = await _n8n.RunWorkflowAsync<N8nCvAnalysisRequest, N8nCvAnalysisPayload>(
                WorkflowPath, payload, cancellationToken);

            var response = Shape(raw);
            CompareWithProfile(response, profile);
            return response;
        }

        // =====================================================================================
        // Confirm: validate everything first, then add through the existing StudentService
        // =====================================================================================

        public async Task<ConfirmCvAnalysisResponse> ConfirmAsync(
            int userId, ConfirmCvAnalysisRequest request, CancellationToken cancellationToken = default)
        {
            var profile = await LoadProfileAsync(userId, cancellationToken);

            var skillsIn = request.Skills ?? new List<AddSkillRequest>();
            var educationIn = request.Education ?? new List<AddEducationRequest>();
            var workIn = request.WorkExperience ?? new List<AddWorkExperienceRequest>();
            var certsIn = request.Certifications ?? new List<AddCertificationRequest>();
            var projectsIn = request.Projects ?? new List<AddProjectRequest>();

            if (skillsIn.Count > MaxSkills || educationIn.Count > MaxEducation || workIn.Count > MaxWorkExperience
                || certsIn.Count > MaxCertifications || projectsIn.Count > MaxProjects)
                throw new InvalidOperationException("Too many items in one request.");

            if (skillsIn.Count + educationIn.Count + workIn.Count + certsIn.Count + projectsIn.Count == 0)
                throw new InvalidOperationException("Nothing selected.");

            var existing = ProfileKeys.From(profile);
            var duplicates = 0;

            // ---- Validate and clean (nothing is saved until all of it is valid) -------------------

            var skills = new List<AddSkillRequest>();
            foreach (var item in skillsIn.Where(i => i != null))
            {
                var name = TextCleaner.Term(item.Name, 60);
                if (name == null) continue;

                if (!existing.Skills.Add(TextCleaner.Key(name))) { duplicates++; continue; }
                skills.Add(new AddSkillRequest { Name = name, ProficiencyLevel = ParseLevel(item.ProficiencyLevel) });
            }

            var education = new List<AddEducationRequest>();
            foreach (var item in educationIn.Where(i => i != null))
            {
                var institution = TextCleaner.Clean(item.Institution, 200);
                var degree = TextCleaner.Clean(item.Degree, 200);
                if (institution == null || degree == null)
                    throw new InvalidOperationException("Each education entry needs an institution and a degree.");

                var label = $"{degree} at {institution}";
                RequireValidStart(item.StartDate, label);

                var endDate = item.IsCurrent ? null : item.EndDate;
                RequireEndAfterStart(item.StartDate, endDate, label);

                if (!existing.Education.Add(TextCleaner.Key(institution, degree))) { duplicates++; continue; }
                education.Add(new AddEducationRequest
                {
                    Institution = institution,
                    Degree = degree,
                    FieldOfStudy = TextCleaner.Clean(item.FieldOfStudy, 200) ?? string.Empty,
                    StartDate = item.StartDate,
                    EndDate = endDate,
                    IsCurrent = item.IsCurrent,
                    Grade = TextCleaner.Clean(item.Grade, 50),
                    CompletionStatus = TextCleaner.Clean(item.CompletionStatus, 50)
                    // SupportingDocumentUrl is never taken from a CV analysis.
                });
            }

            var work = new List<AddWorkExperienceRequest>();
            foreach (var item in workIn.Where(i => i != null))
            {
                var company = TextCleaner.Clean(item.Company, 200);
                var position = TextCleaner.Clean(item.Position, 200);
                if (company == null || position == null)
                    throw new InvalidOperationException("Each work experience entry needs a company and a position.");

                var label = $"{position} at {company}";
                RequireValidStart(item.StartDate, label);

                var endDate = item.IsCurrent ? null : item.EndDate;
                RequireEndAfterStart(item.StartDate, endDate, label);

                if (!existing.WorkExperience.Add(TextCleaner.Key(company, position))) { duplicates++; continue; }
                work.Add(new AddWorkExperienceRequest
                {
                    Company = company,
                    Position = position,
                    Description = TextCleaner.Clean(item.Description, 1000, keepNewlines: true),
                    StartDate = item.StartDate,
                    EndDate = endDate,
                    IsCurrent = item.IsCurrent
                });
            }

            var certifications = new List<AddCertificationRequest>();
            foreach (var item in certsIn.Where(i => i != null))
            {
                var name = TextCleaner.Term(item.Name, 200);
                if (name == null) continue;

                var label = name;
                if (item.IssueDate.HasValue) RequireValidStart(item.IssueDate.Value, label);
                if (item.IssueDate.HasValue) RequireEndAfterStart(item.IssueDate.Value, item.ExpirationDate, label);

                if (!existing.Certifications.Add(TextCleaner.Key(name))) { duplicates++; continue; }
                certifications.Add(new AddCertificationRequest
                {
                    Name = name,
                    IssuingOrganization = TextCleaner.Clean(item.IssuingOrganization, 200) ?? string.Empty,
                    IssueDate = item.IssueDate,
                    ExpirationDate = item.ExpirationDate
                    // CredentialUrl / CertificateFileUrl are never taken from a CV analysis.
                });
            }

            var projects = new List<AddProjectRequest>();
            foreach (var item in projectsIn.Where(i => i != null))
            {
                var title = TextCleaner.Clean(item.Title, 200);
                if (title == null) continue;

                if (!existing.Projects.Add(TextCleaner.Key(title))) { duplicates++; continue; }
                projects.Add(new AddProjectRequest
                {
                    Title = title,
                    Description = TextCleaner.Clean(item.Description, 1000, keepNewlines: true),
                    Role = TextCleaner.Clean(item.Role, 100),
                    Technologies = TextCleaner.Clean(item.Technologies, 300),
                    StartDate = item.StartDate,
                    EndDate = item.EndDate
                    // ProjectUrl / RepositoryUrl are never taken from a CV analysis.
                });
            }

            // ---- Save through the same methods as adding an entry by hand -------------------------

            foreach (var s in skills) await RequireAdded(_students.AddSkillAsync(userId, s));
            foreach (var e in education) await RequireAdded(_students.AddEducationAsync(userId, e));
            foreach (var w in work) await RequireAdded(_students.AddWorkExperienceAsync(userId, w));
            foreach (var c in certifications) await RequireAdded(_students.AddCertificationAsync(userId, c));
            foreach (var p in projects) await RequireAdded(_students.AddProjectAsync(userId, p));

            return new ConfirmCvAnalysisResponse
            {
                SkillsAdded = skills.Count,
                EducationAdded = education.Count,
                WorkExperienceAdded = work.Count,
                CertificationsAdded = certifications.Count,
                ProjectsAdded = projects.Count,
                SkippedAsDuplicate = duplicates
            };
        }

        private static async Task RequireAdded(Task<bool> add)
        {
            if (!await add) throw new InvalidOperationException("Student profile not found.");
        }

        // =====================================================================================
        // Cleaning and validating the untrusted AI output
        // =====================================================================================

        private static CvAnalysisResponse Shape(N8nCvAnalysisPayload raw)
        {
            var response = new CvAnalysisResponse
            {
                Skills = ShapeSkills(raw.Skills),
                Education = ShapeEducation(raw.Education),
                WorkExperience = ShapeWork(raw.WorkExperience),
                Certifications = ShapeCertifications(raw.Certifications),
                Projects = ShapeProjects(raw.Projects),
                Notes = (raw.Notes ?? new List<string>())
                    .Select(n => TextCleaner.Clean(n, 200))
                    .Where(n => !string.IsNullOrEmpty(n))
                    .Select(n => n!)
                    .Take(MaxNotes)
                    .ToList()
            };

            if (response.Skills.Count + response.Education.Count + response.WorkExperience.Count
                + response.Certifications.Count + response.Projects.Count == 0)
                throw new CvAnalysisRejectedException(
                    "No skills, education, experience or certificates could be found in this CV. Check that it is a text PDF and try again.");

            return response;
        }

        private static List<CvSuggestedSkill> ShapeSkills(List<N8nCvSkill>? items)
        {
            var seen = new HashSet<string>();
            var result = new List<CvSuggestedSkill>();

            foreach (var item in items ?? new List<N8nCvSkill>())
            {
                var name = TextCleaner.Term(item?.Name, 60);
                if (name == null || !seen.Add(TextCleaner.Key(name))) continue;

                result.Add(new CvSuggestedSkill { Name = name, ProficiencyLevel = ParseLevel(item!.ProficiencyLevel) });
                if (result.Count >= MaxSkills) break;
            }

            return result;
        }

        private static List<CvSuggestedEducation> ShapeEducation(List<N8nCvEducation>? items)
        {
            var seen = new HashSet<string>();
            var result = new List<CvSuggestedEducation>();

            foreach (var item in items ?? new List<N8nCvEducation>())
            {
                var institution = TextCleaner.Clean(item?.Institution, 200);
                if (institution == null) continue;

                var degree = TextCleaner.Clean(item!.Degree, 200) ?? string.Empty;
                if (!seen.Add(TextCleaner.Key(institution, degree))) continue;

                var (start, end, current) = ShapeDates(item.StartDate, item.EndDate, item.IsCurrent);
                result.Add(new CvSuggestedEducation
                {
                    Institution = institution,
                    Degree = degree,
                    FieldOfStudy = TextCleaner.Clean(item.FieldOfStudy, 200) ?? string.Empty,
                    StartDate = start,
                    EndDate = end,
                    IsCurrent = current,
                    Grade = TextCleaner.Clean(item.Grade, 50),
                    CompletionStatus = TextCleaner.Clean(item.CompletionStatus, 50)
                });
                if (result.Count >= MaxEducation) break;
            }

            return result;
        }

        private static List<CvSuggestedWorkExperience> ShapeWork(List<N8nCvWorkExperience>? items)
        {
            var seen = new HashSet<string>();
            var result = new List<CvSuggestedWorkExperience>();

            foreach (var item in items ?? new List<N8nCvWorkExperience>())
            {
                var company = TextCleaner.Clean(item?.Company, 200);
                var position = TextCleaner.Clean(item?.Position, 200);
                if (company == null || position == null) continue;
                if (!seen.Add(TextCleaner.Key(company, position))) continue;

                var (start, end, current) = ShapeDates(item!.StartDate, item.EndDate, item.IsCurrent);
                result.Add(new CvSuggestedWorkExperience
                {
                    Company = company,
                    Position = position,
                    Description = TextCleaner.Clean(item.Description, 1000, keepNewlines: true),
                    StartDate = start,
                    EndDate = end,
                    IsCurrent = current
                });
                if (result.Count >= MaxWorkExperience) break;
            }

            return result;
        }

        private static List<CvSuggestedCertification> ShapeCertifications(List<N8nCvCertification>? items)
        {
            var seen = new HashSet<string>();
            var result = new List<CvSuggestedCertification>();

            foreach (var item in items ?? new List<N8nCvCertification>())
            {
                var name = TextCleaner.Term(item?.Name, 200);
                if (name == null || !seen.Add(TextCleaner.Key(name))) continue;

                var issued = ParseDate(item!.IssueDate);
                var expires = ParseEndDate(item.ExpirationDate);
                if (issued.HasValue && expires.HasValue && expires < issued) expires = null;

                result.Add(new CvSuggestedCertification
                {
                    Name = name,
                    IssuingOrganization = TextCleaner.Clean(item.IssuingOrganization, 200) ?? string.Empty,
                    IssueDate = issued,
                    ExpirationDate = expires
                });
                if (result.Count >= MaxCertifications) break;
            }

            return result;
        }

        private static List<CvSuggestedProject> ShapeProjects(List<N8nCvProject>? items)
        {
            var seen = new HashSet<string>();
            var result = new List<CvSuggestedProject>();

            foreach (var item in items ?? new List<N8nCvProject>())
            {
                var title = TextCleaner.Clean(item?.Title, 200);
                if (title == null || !seen.Add(TextCleaner.Key(title))) continue;

                result.Add(new CvSuggestedProject
                {
                    Title = title,
                    Description = TextCleaner.Clean(item!.Description, 1000, keepNewlines: true),
                    Role = TextCleaner.Clean(item.Role, 100),
                    Technologies = TextCleaner.Clean(item.Technologies, 300)
                });
                if (result.Count >= MaxProjects) break;
            }

            return result;
        }

        /// <summary>
        /// "Present" (or IsCurrent = true) means current with no end date. An unreadable date becomes null
        /// so the student fills it in, instead of the AI's guess being saved.
        /// </summary>
        private static (DateTime? Start, DateTime? End, bool IsCurrent) ShapeDates(string? start, string? end, bool? isCurrent)
        {
            var current = isCurrent == true
                || (end != null && Array.Exists(CurrentWords, w => string.Equals(w, end.Trim(), StringComparison.OrdinalIgnoreCase)));

            var startDate = ParseDate(start);
            var endDate = current ? null : ParseEndDate(end);

            if (startDate.HasValue && endDate.HasValue && endDate < startDate)
                endDate = null;

            return (startDate, endDate, current);
        }

        private static DateTime? ParseDate(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;

            if (!DateTime.TryParseExact(value.Trim(), DateFormats, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var parsed))
                return null;

            return IsPlausibleYear(parsed.Year) ? parsed : null;
        }

        /// <summary>
        /// An end date written as just a year ("2022") or a month ("2022-06") means the END of that period, so a course
        /// that started and finished in the same year is not read as ending on 1 January.
        /// </summary>
        private static DateTime? ParseEndDate(string? value)
        {
            var parsed = ParseDate(value);
            if (!parsed.HasValue) return null;

            var text = value!.Trim();
            if (text.Length == 4)                       // yyyy
                return new DateTime(parsed.Value.Year, 12, 31);
            if (text.Length == 7)                       // yyyy-MM
                return new DateTime(parsed.Value.Year, parsed.Value.Month,
                    DateTime.DaysInMonth(parsed.Value.Year, parsed.Value.Month));
            return parsed;
        }

        private static bool IsPlausibleYear(int year) => year >= 1950 && year <= DateTime.UtcNow.Year + 8;

        private static string? ParseLevel(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            return ProficiencyLevels.FirstOrDefault(l => string.Equals(l, value.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        private static void RequireValidStart(DateTime date, string label)
        {
            if (date == default || !IsPlausibleYear(date.Year))
                throw new InvalidOperationException($"\"{label}\" needs a valid start date.");
        }

        private static void RequireEndAfterStart(DateTime start, DateTime? end, string label)
        {
            if (end.HasValue && end.Value < start)
                throw new InvalidOperationException($"\"{label}\": the end date is before the start date.");
        }

        private static bool LooksLikePdf(byte[] bytes)
            => bytes.Length >= 5 && bytes[0] == 0x25 && bytes[1] == 0x50 && bytes[2] == 0x44 && bytes[3] == 0x46 && bytes[4] == 0x2D;

        // =====================================================================================
        // Comparing the suggestions with what the profile already has
        // =====================================================================================

        private static void CompareWithProfile(CvAnalysisResponse response, StudentProfile profile)
        {
            var onProfile = ProfileKeys.From(profile);

            foreach (var s in response.Skills)
                if (onProfile.Skills.Contains(TextCleaner.Key(s.Name))) s.Status = CvSuggestionStatus.AlreadyOnProfile;

            foreach (var e in response.Education)
                if (onProfile.Education.Contains(TextCleaner.Key(e.Institution, e.Degree))) e.Status = CvSuggestionStatus.AlreadyOnProfile;

            foreach (var w in response.WorkExperience)
                if (onProfile.WorkExperience.Contains(TextCleaner.Key(w.Company, w.Position))) w.Status = CvSuggestionStatus.AlreadyOnProfile;

            foreach (var c in response.Certifications)
                if (onProfile.Certifications.Contains(TextCleaner.Key(c.Name))) c.Status = CvSuggestionStatus.AlreadyOnProfile;

            foreach (var p in response.Projects)
                if (onProfile.Projects.Contains(TextCleaner.Key(p.Title))) p.Status = CvSuggestionStatus.AlreadyOnProfile;

            // Profile entries this CV does not mention. Only listed for categories where the AI found something,
            // so a missed section does not make the whole profile look "unmentioned".
            var mentioned = new
            {
                Skills = response.Skills.Select(s => TextCleaner.Key(s.Name)).ToHashSet(),
                Education = response.Education.Select(e => TextCleaner.Key(e.Institution, e.Degree)).ToHashSet(),
                Work = response.WorkExperience.Select(w => TextCleaner.Key(w.Company, w.Position)).ToHashSet(),
                Certs = response.Certifications.Select(c => TextCleaner.Key(c.Name)).ToHashSet(),
                Projects = response.Projects.Select(p => TextCleaner.Key(p.Title)).ToHashSet()
            };

            if (response.Skills.Count > 0)
                response.NotMentionedInCv.Skills = (profile.Skills ?? new List<Skill>())
                    .Where(s => !mentioned.Skills.Contains(TextCleaner.Key(s.Name)))
                    .Select(s => s.Name).ToList();

            if (response.Education.Count > 0)
                response.NotMentionedInCv.Education = (profile.Education ?? new List<Education>())
                    .Where(e => !mentioned.Education.Contains(TextCleaner.Key(e.Institution, e.Degree)))
                    .Select(e => $"{e.Degree} at {e.Institution}").ToList();

            if (response.WorkExperience.Count > 0)
                response.NotMentionedInCv.WorkExperience = (profile.WorkExperience ?? new List<WorkExperience>())
                    .Where(w => !mentioned.Work.Contains(TextCleaner.Key(w.Company, w.Position)))
                    .Select(w => $"{w.Position} at {w.Company}").ToList();

            if (response.Certifications.Count > 0)
                response.NotMentionedInCv.Certifications = (profile.Certifications ?? new List<StudentCertification>())
                    .Where(c => !mentioned.Certs.Contains(TextCleaner.Key(c.Name)))
                    .Select(c => c.Name).ToList();

            if (response.Projects.Count > 0)
                response.NotMentionedInCv.Projects = (profile.Projects ?? new List<StudentProject>())
                    .Where(p => !mentioned.Projects.Contains(TextCleaner.Key(p.Title)))
                    .Select(p => p.Title).ToList();
        }

        /// <summary>Comparison keys of everything already on a profile.</summary>
        private sealed class ProfileKeys
        {
            public HashSet<string> Skills { get; } = new();
            public HashSet<string> Education { get; } = new();
            public HashSet<string> WorkExperience { get; } = new();
            public HashSet<string> Certifications { get; } = new();
            public HashSet<string> Projects { get; } = new();

            public static ProfileKeys From(StudentProfile profile)
            {
                var keys = new ProfileKeys();
                foreach (var s in profile.Skills ?? new List<Skill>()) keys.Skills.Add(TextCleaner.Key(s.Name));
                foreach (var e in profile.Education ?? new List<Education>()) keys.Education.Add(TextCleaner.Key(e.Institution, e.Degree));
                foreach (var w in profile.WorkExperience ?? new List<WorkExperience>()) keys.WorkExperience.Add(TextCleaner.Key(w.Company, w.Position));
                foreach (var c in profile.Certifications ?? new List<StudentCertification>()) keys.Certifications.Add(TextCleaner.Key(c.Name));
                foreach (var p in profile.Projects ?? new List<StudentProject>()) keys.Projects.Add(TextCleaner.Key(p.Title));
                return keys;
            }
        }

        private async Task<StudentProfile> LoadProfileAsync(int userId, CancellationToken cancellationToken)
        {
            var profile = await _context.StudentProfiles
                .AsNoTracking()
                .Include(s => s.Education)
                .Include(s => s.WorkExperience)
                .Include(s => s.Skills)
                .Include(s => s.Certifications)
                .Include(s => s.Projects)
                .FirstOrDefaultAsync(s => s.UserId == userId && !s.IsDeleted, cancellationToken);

            return profile ?? throw new InvalidOperationException("Student profile not found.");
        }
    }
}
