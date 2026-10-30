using InternshipManagement.Application.DTOs.Internship;
using InternshipManagement.Application.Interfaces;
using InternshipManagement.Domain.Entities;
using InternshipManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace InternshipManagement.Application.Services
{
    /// <summary>Thrown when n8n/Gemini answered, but the draft is not usable.</summary>
    public sealed class AiDraftRejectedException : Exception
    {
        public AiDraftRejectedException(string message) : base(message) { }
    }

    public class InternshipDraftService : IInternshipDraftService
    {
        // Must match the Webhook node path in n8n/internship-generate.workflow.json
        private const string WorkflowPath = "internship/generate";
        private const int MaxSavedDrafts = 20;

        private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

        private static readonly Regex HtmlTags = new("<[^>]*>", RegexOptions.Compiled);
        private static readonly Regex Spaces = new("[ \\t]+", RegexOptions.Compiled);
        private static readonly Regex ExtraNewlines = new("\\n{3,}", RegexOptions.Compiled);

        private readonly IApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IN8nClient _n8n;
        private readonly IRunTokenService _runTokens;

        public InternshipDraftService(
            IApplicationDbContext context, IConfiguration configuration, IN8nClient n8n, IRunTokenService runTokens)
        {
            _context = context;
            _configuration = configuration;
            _n8n = n8n;
            _runTokens = runTokens;
        }

        public async Task<InternshipDraftResponse> GenerateDraftAsync(
            int userId, GenerateInternshipDraftRequest request, CancellationToken cancellationToken = default)
        {
            // Same gate as CreateInternshipAsync: only companies that may post can spend AI quota.
            var company = await _context.CompanyProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);

            if (company == null)
                throw new InvalidOperationException("Company profile not found.");

            if (company.VerificationStatus != CompanyVerificationStatus.Verified)
                throw new InvalidOperationException("Company must be verified to create internships.");

            if (_configuration.GetValue<bool>("Internships:RequireSubscription") && !company.IsSubscribed)
                throw new InvalidOperationException("Active subscription required.");

            // Check the limit before spending AI quota.
            var saved = await _context.InternshipDrafts.CountAsync(d => d.CompanyProfileId == company.Id, cancellationToken);
            if (saved >= MaxSavedDrafts)
                throw new InvalidOperationException(
                    $"You have {MaxSavedDrafts} saved drafts. Delete one before generating another.");

            // Send n8n only what the workflow needs: no company id, name, email or user id.
            var payload = new N8nInternshipDraftRequest
            {
                Prompt = request.Prompt.Trim(),
                CompanyIndustry = company.Industry,
                AllowedInternshipTypes = Enum.GetNames<InternshipType>(),
                AllowedDurations = Enum.GetNames<InternshipDuration>(),
                // Lets the AI's tools call back as this company for a few minutes (read-only reference data).
                RunToken = _runTokens.Create(userId, "Company", "draft")
            };
           
            var raw = await _n8n.RunWorkflowAsync<N8nInternshipDraftRequest, N8nInternshipDraftPayload>(
                WorkflowPath, payload, cancellationToken);

            var draft = Shape(raw, company.Industry);

            var entity = new InternshipDraft
            {
                CompanyProfileId = company.Id,
                Title = draft.Title,
                Prompt = request.Prompt.Trim(),
                DraftJson = JsonSerializer.Serialize(draft, JsonOpts),
                CreatedBy = userId.ToString()
            };
            _context.InternshipDrafts.Add(entity);
            await _context.SaveChangesAsync(cancellationToken);

            return Hydrate(entity, draft);
        }

        // ---- Saved drafts ----------------------------------------------------------------------

        public async Task<List<InternshipDraftSummary>> ListDraftsAsync(int userId, CancellationToken cancellationToken = default)
        {
            var companyId = await GetCompanyIdAsync(userId, cancellationToken);

            return await _context.InternshipDrafts
                .AsNoTracking()
                .Where(d => d.CompanyProfileId == companyId)
                .OrderByDescending(d => d.UpdatedAt ?? d.CreatedAt)
                .Select(d => new InternshipDraftSummary
                {
                    Id = d.Id,
                    Title = d.Title,
                    CreatedAt = d.CreatedAt,
                    UpdatedAt = d.UpdatedAt
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<InternshipDraftResponse?> GetDraftAsync(int userId, int draftId, CancellationToken cancellationToken = default)
        {
            var companyId = await GetCompanyIdAsync(userId, cancellationToken);
            var entity = await _context.InternshipDrafts
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == draftId && d.CompanyProfileId == companyId, cancellationToken);

            return entity == null ? null : Hydrate(entity, Deserialize(entity.DraftJson));
        }

        public async Task<InternshipDraftResponse?> UpdateDraftAsync(
            int userId, int draftId, SaveInternshipDraftRequest request, CancellationToken cancellationToken = default)
        {
            var companyId = await GetCompanyIdAsync(userId, cancellationToken);
            var entity = await _context.InternshipDrafts
                .FirstOrDefaultAsync(d => d.Id == draftId && d.CompanyProfileId == companyId, cancellationToken);

            if (entity == null) return null;

            var title = Clean(request.Title, 200);
            var description = Clean(request.Description, 2000, keepNewlines: true);
            if (string.IsNullOrEmpty(title)) throw new InvalidOperationException("Title is required.");
            if (string.IsNullOrEmpty(description)) throw new InvalidOperationException("Description is required.");

            var stored = Deserialize(entity.DraftJson);

            stored.Title = title;
            stored.Description = description;
            stored.Responsibilities = Clean(request.Responsibilities, 2000, keepNewlines: true);
            stored.RequiredQualifications = Clean(request.RequiredQualifications, 2000);
            stored.PreferredQualifications = Clean(request.PreferredQualifications, 2000);
            stored.RequiredSkills = Clean(request.RequiredSkills, 1000);
            stored.PreferredSkills = Clean(request.PreferredSkills, 1000);
            stored.Industry = Clean(request.Industry, 100) ?? string.Empty;
            stored.Location = Clean(request.Location, 200);
            stored.IsRemote = request.IsRemote;
            stored.InternshipType = ParseEnum<InternshipType>(request.InternshipType);
            stored.Duration = ParseEnum<InternshipDuration>(request.Duration);
            stored.EligibleProgrammes = (request.EligibleProgrammes ?? new List<string>())
                .Select(p => Clean(p, 100)).Where(p => !string.IsNullOrEmpty(p)).Select(p => p!)
                .Distinct(StringComparer.OrdinalIgnoreCase).Take(10).ToList();
            stored.StartDate = request.StartDate;
            stored.ApplicationDeadline = request.ApplicationDeadline;
            stored.NumberOfPositions = request.NumberOfPositions;

            entity.Title = title;
            entity.DraftJson = JsonSerializer.Serialize(stored, JsonOpts);
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = userId.ToString();
            await _context.SaveChangesAsync(cancellationToken);

            return Hydrate(entity, stored);
        }

        public async Task<bool> DeleteDraftAsync(int userId, int draftId, CancellationToken cancellationToken = default)
        {
            var companyId = await GetCompanyIdAsync(userId, cancellationToken);
            var entity = await _context.InternshipDrafts
                .FirstOrDefaultAsync(d => d.Id == draftId && d.CompanyProfileId == companyId, cancellationToken);

            if (entity == null) return false;

            entity.IsDeleted = true;   // soft delete, like the other entities
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = userId.ToString();
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        private async Task<int> GetCompanyIdAsync(int userId, CancellationToken cancellationToken)
        {
            var companyId = await _context.CompanyProfiles
                .AsNoTracking()
                .Where(c => c.UserId == userId)
                .Select(c => (int?)c.Id)
                .FirstOrDefaultAsync(cancellationToken);

            return companyId ?? throw new InvalidOperationException("Company profile not found.");
        }

        private static InternshipDraftResponse Deserialize(string json)
            => JsonSerializer.Deserialize<InternshipDraftResponse>(json, JsonOpts) ?? new InternshipDraftResponse();

        /// <summary>Adds the database values and recomputes which fields the company still has to fill in.</summary>
        private static InternshipDraftResponse Hydrate(InternshipDraft entity, InternshipDraftResponse draft)
        {
            draft.Id = entity.Id;
            draft.CreatedAt = entity.CreatedAt;
            draft.UpdatedAt = entity.UpdatedAt;

            var toComplete = new List<string>();
            if (draft.StartDate is null) toComplete.Add("StartDate");
            if (draft.ApplicationDeadline is null) toComplete.Add("ApplicationDeadline");
            if (draft.NumberOfPositions is null) toComplete.Add("NumberOfPositions");
            if (draft.InternshipType is null) toComplete.Add("InternshipType");
            if (draft.Duration is null) toComplete.Add("Duration");
            if (string.IsNullOrEmpty(draft.Industry)) toComplete.Add("Industry");
            if (!draft.IsRemote && string.IsNullOrEmpty(draft.Location)) toComplete.Add("Location");
            draft.FieldsToComplete = toComplete;

            return draft;
        }

        /// <summary>Joins items without ever exceeding the database column length.</summary>
        private static string JoinCapped(IEnumerable<string> items, string separator, int maxLength)
        {
            var sb = new StringBuilder();
            foreach (var item in items)
            {
                var extra = (sb.Length == 0 ? 0 : separator.Length) + item.Length;
                if (sb.Length + extra > maxLength) break;
                if (sb.Length > 0) sb.Append(separator);
                sb.Append(item);
            }
            return sb.ToString();
        }

        // ---- Validation and cleaning of the untrusted AI output ----------------------------------

        private static InternshipDraftResponse Shape(N8nInternshipDraftPayload raw, string? companyIndustry)
        {
            var title = Clean(raw.Title, 200);
            var description = Clean(raw.Description, 2000, keepNewlines: true);

            // Same minimum as CreateInternshipValidator, so a draft never fails on submit for this.
            if (string.IsNullOrWhiteSpace(title) || description is null || description.Length < 50)
                throw new AiDraftRejectedException(
                    "The AI did not return a usable draft. Add more detail to your description and try again.");

            var requiredSkills = Terms(raw.RequiredSkills);
            var requiredQualifications = Terms(raw.RequiredQualifications);

            // A term listed as required is not repeated as preferred.
            var preferredSkills = Terms(raw.PreferredSkills)
                .Where(t => !requiredSkills.Contains(t, StringComparer.OrdinalIgnoreCase)).ToList();
            var preferredQualifications = Terms(raw.PreferredQualifications)
                .Where(t => !requiredQualifications.Contains(t, StringComparer.OrdinalIgnoreCase)).ToList();

            var responsibilities = (raw.Responsibilities ?? new List<string>())
                .Select(r => Clean(r, 300))
                .Where(r => !string.IsNullOrEmpty(r))
                .Take(10)
                .Select(r => "- " + r)
                .ToList();

            var industry = Clean(raw.Industry, 100) ?? Clean(companyIndustry, 100) ?? string.Empty;
            var location = Clean(raw.Location, 200);
            var isRemote = raw.IsRemote ?? false;
            var internshipType = ParseEnum<InternshipType>(raw.InternshipType);
            var duration = ParseEnum<InternshipDuration>(raw.Duration);

            var programmes = (raw.EligibleProgrammes ?? new List<string>())
                .Select(p => Clean(p, 100))
                .Where(p => !string.IsNullOrEmpty(p))
                .Select(p => p!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(10)
                .ToList();

            var missing = (raw.MissingDetails ?? new List<string>())
                .Select(m => Clean(m, 200))
                .Where(m => !string.IsNullOrEmpty(m))
                .Select(m => m!)
                .Take(8)
                .ToList();

            // The AI never invents these, so the company always has to set them.
            var toComplete = new List<string> { "StartDate", "ApplicationDeadline", "NumberOfPositions" };
            if (internshipType is null) toComplete.Add("InternshipType");
            if (duration is null) toComplete.Add("Duration");
            if (string.IsNullOrEmpty(industry)) toComplete.Add("Industry");
            if (!isRemote && string.IsNullOrEmpty(location)) toComplete.Add("Location");

            return new InternshipDraftResponse
            {
                Title = title,
                Description = description,
                Responsibilities = responsibilities.Count == 0 ? null : JoinCapped(responsibilities, "\n", 2000),
                RequiredQualifications = requiredQualifications.Count == 0 ? null : JoinCapped(requiredQualifications, ", ", 2000),
                PreferredQualifications = preferredQualifications.Count == 0 ? null : JoinCapped(preferredQualifications, ", ", 2000),
                RequiredSkills = requiredSkills.Count == 0 ? null : JoinCapped(requiredSkills, ", ", 1000),
                PreferredSkills = preferredSkills.Count == 0 ? null : JoinCapped(preferredSkills, ", ", 1000),
                Industry = industry,
                Location = location,
                IsRemote = isRemote,
                InternshipType = internshipType,
                Duration = duration,
                EligibleProgrammes = programmes,
                FieldsToComplete = toComplete,
                MissingDetails = missing,
                IsAiGenerated = true
            };
        }

        /// <summary>
        /// Short terms only: MatchingService splits these fields on commas, dots, dashes and slashes,
        /// so each term is trimmed, de-duplicated, length-capped and has any comma removed.
        /// </summary>
        private static List<string> Terms(List<string>? items, int maxItems = 15, int maxLength = 80)
        {
            return (items ?? new List<string>())
                .Select(i => Clean(i, maxLength))
                .Where(i => !string.IsNullOrEmpty(i))
                .Select(i => i!.Replace(",", " ").Trim())
                .Where(i => i.Length >= 2)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(maxItems)
                .ToList();
        }

        private static string? ParseEnum<T>(string? value) where T : struct, Enum
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            var text = value.Trim();

            // Reject numeric strings such as "1", which Enum.TryParse would accept as a value.
            if (int.TryParse(text, out _)) return null;

            return Enum.TryParse<T>(text, true, out var parsed) && Enum.IsDefined(parsed)
                ? parsed.ToString()
                : null;
        }

        /// <summary>Strips HTML tags and control characters, collapses whitespace, caps the length.</summary>
        private static string? Clean(string? value, int maxLength, bool keepNewlines = false)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;

            var text = HtmlTags.Replace(value, " ");

            var sb = new StringBuilder(text.Length);
            foreach (var ch in text)
            {
                if (ch == '\n' && keepNewlines) sb.Append('\n');
                else if (char.IsControl(ch)) sb.Append(' ');
                else sb.Append(ch);
            }

            text = Spaces.Replace(sb.ToString(), " ");
            if (keepNewlines)
            {
                text = Regex.Replace(text, " ?\\n ?", "\n");
                text = ExtraNewlines.Replace(text, "\n\n");
            }

            text = text.Trim();
            if (text.Length == 0) return null;

            return text.Length > maxLength ? text[..maxLength].TrimEnd() : text;
        }
    }
}
