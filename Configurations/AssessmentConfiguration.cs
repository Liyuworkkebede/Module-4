using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TmsApi.Entities;

namespace TmsApi.Configurations;

public class AssessmentConfiguration : IEntityTypeConfiguration<Assessment>
{
    public void Configure(EntityTypeBuilder<Assessment> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedOnAdd();

        builder.Property(a => a.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(a => a.MaxScore)
            .IsRequired()
            .HasPrecision(5, 2);

        builder.Property(a => a.Weight)
            .IsRequired()
            .HasPrecision(3, 2);

        builder.Property(a => a.CourseId)
            .IsRequired();

        builder.HasIndex(a => a.CourseId);

        // ✅ Configure relationship HERE
        builder.HasOne(a => a.Course)
            .WithMany(c => c.Assessments)
            .HasForeignKey(a => a.CourseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable("Assessments");
    }
}