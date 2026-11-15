using InternshipManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InternshipManagement.Infrastructure.Persistence.Configurations
{
    public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
    {
        public void Configure(EntityTypeBuilder<Notification> builder)
        {
            builder.ToTable("Notifications");
            builder.HasKey(n => n.Id);

            builder.Property(n => n.Type).IsRequired().HasMaxLength(50);
            builder.Property(n => n.Title).IsRequired().HasMaxLength(200);
            builder.Property(n => n.Message).IsRequired().HasMaxLength(1000);
            builder.Property(n => n.LinkPath).HasMaxLength(300);

            // the list for one user (newest first) and the unread count
            builder.HasIndex(n => new { n.UserId, n.IsRead, n.CreatedAt });

            // clearing out old notifications
            builder.HasIndex(n => n.CreatedAt);
        }
    }
}
