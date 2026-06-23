using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TmsApi.Entities;

namespace TmsApi.Configurations;

public class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedOnAdd();

        builder.Property(c => c.Code)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(c => c.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.Capacity)
            .IsRequired()
            .HasDefaultValue(30);

        builder.HasIndex(c => c.Code)
            .IsUnique();

        builder.HasIndex(c => c.Title);

        // ✅ REMOVE relationship configuration from here
        // We'll configure it in EnrollmentConfiguration

        builder.ToTable("Courses");
    }
}