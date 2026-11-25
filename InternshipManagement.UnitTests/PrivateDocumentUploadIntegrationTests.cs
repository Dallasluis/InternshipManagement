using InternshipManagement.Application.Interfaces;
using InternshipManagement.Application.Services;
using InternshipManagement.Domain.Entities;
using InternshipManagement.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InternshipManagement.UnitTests;

public sealed class PrivateDocumentUploadIntegrationTests
{
    [Fact]
    public async Task UploadAsync_ScansStoresAndPersistsPrivateDocument()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<SqliteDocumentTestContext>().UseSqlite(connection).Options;
        await using var context = new SqliteDocumentTestContext(options);
        await context.Database.EnsureCreatedAsync();
        var storage = new MemoryPrivateDocumentStorage();
        var scanner = new RecordingMalwareScanner();
        var service = new PrivateDocumentService(context, storage, scanner);
        await using var content = new MemoryStream("%PDF-1.7 test document"u8.ToArray());

        var result = await service.UploadAsync(42, "Student", "Resume", "resume.pdf",
            content.Length, content, CancellationToken.None);

        Assert.Equal("Resume", result.Category);
        Assert.Equal("Active", result.RetentionState);
        Assert.True(scanner.WasCalled);
        Assert.Equal(content.ToArray(), storage.Files[result.Id]);
        Assert.True(await context.PrivateDocuments.AnyAsync(document => document.Id == result.Id));
        Assert.True(await context.DocumentAccessAudits.AnyAsync(audit => audit.DocumentId == result.Id));
    }

    [Fact]
    public async Task UploadAsync_DoesNotStoreOrPersistWhenScannerRejectsFile()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<SqliteDocumentTestContext>().UseSqlite(connection).Options;
        await using var context = new SqliteDocumentTestContext(options);
        await context.Database.EnsureCreatedAsync();
        var storage = new MemoryPrivateDocumentStorage();
        var service = new PrivateDocumentService(context, storage, new RejectingMalwareScanner());
        await using var content = new MemoryStream("%PDF-1.7 infected document"u8.ToArray());

        await Assert.ThrowsAsync<InvalidDataException>(() => service.UploadAsync(
            42, "Student", "Resume", "resume.pdf", content.Length, content, CancellationToken.None));

        Assert.Empty(storage.Files);
        Assert.Empty(await context.PrivateDocuments.ToListAsync());
    }

    private sealed class RecordingMalwareScanner : IFileMalwareScanner
    {
        public bool WasCalled { get; private set; }
        public Task ScanAsync(Stream content, CancellationToken cancellationToken)
        {
            WasCalled = true;
            return Task.CompletedTask;
        }
    }

    private sealed class RejectingMalwareScanner : IFileMalwareScanner
    {
        public Task ScanAsync(Stream content, CancellationToken cancellationToken) =>
            throw new InvalidDataException("Malware detected.");
    }

    private sealed class MemoryPrivateDocumentStorage : IPrivateDocumentStorage
    {
        public Dictionary<Guid, byte[]> Files { get; } = new();

        public async Task SaveAsync(Guid documentId, Stream content, CancellationToken cancellationToken) =>
            Files.Add(documentId, await ReadAllBytesAsync(content, cancellationToken));

        public Task<Stream> OpenReadAsync(Guid documentId, CancellationToken cancellationToken) =>
            Task.FromResult<Stream>(new MemoryStream(Files[documentId], writable: false));

        public Task DeleteAsync(Guid documentId, CancellationToken cancellationToken)
        {
            Files.Remove(documentId);
            return Task.CompletedTask;
        }

        private static async Task<byte[]> ReadAllBytesAsync(Stream content, CancellationToken cancellationToken)
        {
            using var output = new MemoryStream();
            await content.CopyToAsync(output, cancellationToken);
            return output.ToArray();
        }
    }

    private sealed class SqliteDocumentTestContext(DbContextOptions<SqliteDocumentTestContext> options)
        : DbContext(options), IApplicationDbContext
    {
        public DbSet<StudentProfile> StudentProfiles => Set<StudentProfile>();
        public DbSet<Education> Educations => Set<Education>();
        public DbSet<WorkExperience> WorkExperiences => Set<WorkExperience>();
        public DbSet<Skill> Skills => Set<Skill>();
        public DbSet<StudentCertification> StudentCertifications => Set<StudentCertification>();
        public DbSet<StudentProject> StudentProjects => Set<StudentProject>();
        public DbSet<CompanyProfile> CompanyProfiles => Set<CompanyProfile>();
        public DbSet<CompanyRepresentative> CompanyRepresentatives => Set<CompanyRepresentative>();
        public DbSet<Internship> Internships => Set<Internship>();
        public DbSet<InternshipDraft> InternshipDrafts => Set<InternshipDraft>();
        public DbSet<AiJob> AiJobs => Set<AiJob>();
        public DbSet<InternshipApplication> InternshipApplications => Set<InternshipApplication>();
        public DbSet<ApplicationStatusHistory> ApplicationStatusHistories => Set<ApplicationStatusHistory>();
        public DbSet<Placement> Placements => Set<Placement>();
        public DbSet<ProgressReport> ProgressReports => Set<ProgressReport>();
        public DbSet<InternshipExtensionRequest> InternshipExtensionRequests => Set<InternshipExtensionRequest>();
        public DbSet<InternshipWithdrawalRequest> InternshipWithdrawalRequests => Set<InternshipWithdrawalRequest>();
        public DbSet<InternshipEvaluation> InternshipEvaluations => Set<InternshipEvaluation>();
        public DbSet<Report> Reports => Set<Report>();
        public DbSet<AccountSecurityEvent> AccountSecurityEvents => Set<AccountSecurityEvent>();
        public DbSet<EmailActionToken> EmailActionTokens => Set<EmailActionToken>();
        public DbSet<CompanyVerificationHistory> CompanyVerificationHistories => Set<CompanyVerificationHistory>();
        public DbSet<PrivateDocument> PrivateDocuments => Set<PrivateDocument>();
        public DbSet<DocumentAccessToken> DocumentAccessTokens => Set<DocumentAccessToken>();
        public DbSet<DocumentAccessAudit> DocumentAccessAudits => Set<DocumentAccessAudit>();
        public DbSet<Notification> Notifications => Set<Notification>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Ignore<StudentProfile>();
            modelBuilder.Ignore<Education>();
            modelBuilder.Ignore<WorkExperience>();
            modelBuilder.Ignore<Skill>();
            modelBuilder.Ignore<StudentCertification>();
            modelBuilder.Ignore<StudentProject>();
            modelBuilder.Ignore<CompanyProfile>();
            modelBuilder.Ignore<CompanyRepresentative>();
            modelBuilder.Ignore<Internship>();
            modelBuilder.Ignore<InternshipDraft>();
            modelBuilder.Ignore<AiJob>();
            modelBuilder.Ignore<InternshipApplication>();
            modelBuilder.Ignore<ApplicationStatusHistory>();
            modelBuilder.Ignore<Placement>();
            modelBuilder.Ignore<ProgressReport>();
            modelBuilder.Ignore<InternshipExtensionRequest>();
            modelBuilder.Ignore<InternshipWithdrawalRequest>();
            modelBuilder.Ignore<InternshipEvaluation>();
            modelBuilder.Ignore<Report>();
            modelBuilder.Ignore<AccountSecurityEvent>();
            modelBuilder.Ignore<EmailActionToken>();
            modelBuilder.Ignore<CompanyVerificationHistory>();
            modelBuilder.Ignore<Notification>();
            modelBuilder.Entity<PrivateDocument>().HasKey(document => document.Id);
            modelBuilder.Entity<DocumentAccessToken>().HasKey(token => token.TokenHash);
            modelBuilder.Entity<DocumentAccessAudit>().HasKey(audit => audit.Id);
        }
    }
}
