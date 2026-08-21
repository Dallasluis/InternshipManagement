using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InternshipManagement.Domain.Entities;
using InternshipManagement.Domain.Enums;

namespace InternshipManagement.Domain.Configurations
{
    public class InternshipConfiguration : IEntityTypeConfiguration<Internship>
    {
        public void Configure(EntityTypeBuilder<Internship> builder)
        {
            builder.ToTable("Internships");

            builder.HasKey(i => i.Id);

            builder.Property(i => i.Title)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(i => i.Description)
                .IsRequired()
                .HasMaxLength(2000);

            builder.Property(i => i.Responsibilities)
                .HasMaxLength(2000);

            builder.Property(i => i.Requirements)
                .HasMaxLength(2000);

            builder.Property(i => i.Qualifications)
                .HasMaxLength(2000);

            builder.Property(i => i.Skills)
                .HasMaxLength(500);

            builder.Property(i => i.Industry)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(i => i.Location)
                .HasMaxLength(200);

            builder.Property(i => i.Compensation)
                .HasMaxLength(50);

            builder.Property(i => i.Currency)
                .HasMaxLength(10);

            builder.Property(i => i.EligibleProgrammes)
                .HasColumnType("nvarchar(max)");

            builder.Property(i => i.Status)
                .HasDefaultValue(InternshipStatus.Draft);

            builder.Property(i => i.ModerationStatus)
                .HasDefaultValue(ModerationStatus.Pending);

            builder.Property(i => i.Views)
                .HasDefaultValue(0);

            builder.Property(i => i.ApplicationsCount)
                .HasDefaultValue(0);

            // Relationships
            builder.HasOne(i => i.CompanyProfile)
                .WithMany(c => c.Internships)
                .HasForeignKey(i => i.CompanyProfileId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(i => i.InternshipApplications)
                .WithOne(a => a.Internship)
                .HasForeignKey(a => a.InternshipId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            builder.HasIndex(i => i.CompanyProfileId);
            builder.HasIndex(i => i.Status);
            builder.HasIndex(i => i.ModerationStatus);
            builder.HasIndex(i => i.Industry);
            builder.HasIndex(i => i.Location);
            builder.HasIndex(i => i.CreatedAt);
            builder.HasIndex(i => i.ApplicationDeadline);
        }
    }
}