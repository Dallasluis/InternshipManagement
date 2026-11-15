using InternshipManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InternshipManagement.Infrastructure.Persistence.Configurations
{
    public class SavedInternshipConfiguration : IEntityTypeConfiguration<SavedInternship>
    {
        public void Configure(EntityTypeBuilder<SavedInternship> builder)
        {
            builder.ToTable("SavedInternships");
            builder.HasKey(s => s.Id);
            builder.HasIndex(s => new { s.UserId, s.InternshipId }).IsUnique();
            builder.HasIndex(s => s.InternshipId);
        }
    }

    public class CompanyWarningConfiguration : IEntityTypeConfiguration<CompanyWarning>
    {
        public void Configure(EntityTypeBuilder<CompanyWarning> builder)
        {
            builder.ToTable("CompanyWarnings");
            builder.HasKey(w => w.Id);
            builder.Property(w => w.Message).IsRequired().HasMaxLength(2000);
            builder.HasIndex(w => new { w.CompanyProfileId, w.CreatedAt });
        }
    }

    public class InternshipCategoryConfiguration : IEntityTypeConfiguration<InternshipCategory>
    {
        public void Configure(EntityTypeBuilder<InternshipCategory> builder)
        {
            builder.ToTable("InternshipCategories");
            builder.HasKey(c => c.Id);
            builder.Property(c => c.Name).IsRequired().HasMaxLength(100);
            builder.HasIndex(c => c.Name).IsUnique();
        }
    }
}
