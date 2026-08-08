using TmsApi.Dtos;
using TmsApi.Entities;

namespace TmsApi.Services;

public interface ICourseService
{
    Task<CourseResponseDto?> GetByIdAsync(int id, CancellationToken ct);
    Task<CourseResponseDto> CreateAsync(CreateCourseRequest request, CancellationToken ct);
    Task<bool> CodeExistsAsync(string code, CancellationToken ct);
    Task<PagedResponse<CourseResponseDto>> GetCoursesAsync(PagedRequest request, CancellationToken ct);

    /// <summary>Returns the full Course entity including Enrollments (for capacity checks).</summary>
    Task<Course?> GetByCodeAsync(string code, CancellationToken ct);
}
