using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InternshipManagement.Domain.Entities;
using InternshipManagement.Domain.Enums;

namespace InternshipManagement.Domain.Configurations
{
    public class CompanyProfileConfiguration : IEntityTypeConfiguration<CompanyProfile>
    {
        public void Configure(EntityTypeBuilder<CompanyProfile> builder)
        {
            builder.ToTable("CompanyProfiles");

            builder.HasKey(c => c.Id);

            builder.Property(c => c.CompanyName)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(c => c.Description)
                .HasMaxLength(2000);

            builder.Property(c => c.Industry)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(c => c.Website)
                .HasMaxLength(255);

            builder.Property(c => c.LinkedInUrl)
                .HasMaxLength(255);

            builder.Property(c => c.LogoUrl)
                .HasMaxLength(500);

            builder.Property(c => c.Address)
                .HasMaxLength(500);

            builder.Property(c => c.City)
                .HasMaxLength(100);

            builder.Property(c => c.Country)
                .HasMaxLength(100);

            builder.Property(c => c.PhoneNumber)
                .HasMaxLength(20);

            builder.Property(c => c.VerificationStatus)
                .HasDefaultValue(CompanyVerificationStatus.Pending);

            builder.Property(c => c.VerificationDocuments)
                .HasColumnType("nvarchar(max)");

            builder.Property(c => c.AdminNotes)
                .HasMaxLength(1000);

            builder.Property(c => c.IsSubscribed)
                .HasDefaultValue(false);

            // Indexes
            builder.HasIndex(c => c.UserId).IsUnique();
            builder.HasIndex(c => c.VerificationStatus);
            builder.HasIndex(c => c.Industry);
            builder.HasIndex(c => c.Country);
            builder.HasIndex(c => c.City);

            // Relationships
            builder.HasMany(c => c.Internships)
                .WithOne(i => i.CompanyProfile)
                .HasForeignKey(i => i.CompanyProfileId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}