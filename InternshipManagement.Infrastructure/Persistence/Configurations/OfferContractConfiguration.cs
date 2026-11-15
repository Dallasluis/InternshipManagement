using InternshipManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InternshipManagement.Infrastructure.Persistence.Configurations
{
    public class OfferContractConfiguration : IEntityTypeConfiguration<OfferContract>
    {
        public void Configure(EntityTypeBuilder<OfferContract> builder)
        {
            builder.ToTable("OfferContracts");
            builder.HasKey(c => c.Id);
            builder.Property(c => c.FileName).IsRequired().HasMaxLength(255);
            builder.Property(c => c.ContentType).IsRequired().HasMaxLength(150);
            builder.Property(c => c.Content).HasColumnType("varbinary(max)");
            builder.HasIndex(c => c.InternshipApplicationId).IsUnique();
        }
    }
}
