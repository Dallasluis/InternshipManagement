using InternshipManagement.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace InternshipManagement.Application.Services
{
    /// <summary>
    /// Reference data for the CV analysis agent. Read-only, not tied to any one student: only certification
    /// NAMES come back, never who holds them, so the agent can match "Microsoft Azure Fundamentals" on a CV
    /// to the spelling the platform already uses.
    /// </summary>
    public class CvAgentToolsService : ICvAgentToolsService
    {
        private const int MaxCertifications = 100;
        private readonly IApplicationDbContext _context;

        public CvAgentToolsService(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<CertificationVocabularyResponse> GetCertificationVocabularyAsync(
            CancellationToken cancellationToken = default)
        {
            var names = await _context.StudentCertifications
                .AsNoTracking()
                .OrderBy(c => c.Id)
                .Select(c => c.Name)
                .Take(2000)
                .ToListAsync(cancellationToken);

            var ranked = names
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Select(n => n.Trim())
                .GroupBy(n => n, StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(g => g.Count())
                .ThenBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.Key)
                .Take(MaxCertifications)
                .ToArray();

            return new CertificationVocabularyResponse { Certifications = ranked };
        }
    }
}
