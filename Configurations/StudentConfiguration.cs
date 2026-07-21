using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TmsApi.Entities;

namespace TmsApi.Configurations;

public class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedOnAdd();

        builder.Property(s => s.RegistrationNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.GPA)
            .HasPrecision(3, 2)
            .HasDefaultValue(0.0m);

        builder.Property(s => s.IsActive)
            .HasDefaultValue(true);

        builder.Property(s => s.IsDeleted)
            .HasDefaultValue(false);

        // ✅ Concurrency token
        builder.Property(s => s.Version)
            .IsRowVersion();  // Maps to PostgreSQL xmin

        // ✅ Shadow property for audit
        builder.Property<DateTime>("LastUpdated")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // ✅ Soft delete filter
        builder.HasQueryFilter(s => !s.IsDeleted);

        builder.HasIndex(s => s.RegistrationNumber)
            .IsUnique();

        builder.HasIndex(s => s.Name);

        builder.ToTable("Students");
    }
}