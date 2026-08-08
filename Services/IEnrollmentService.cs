using TmsApi.Dtos;
using TmsApi.Entities;

namespace TmsApi.Services;

public interface IEnrollmentService
{
    Task<EnrollmentResponseDto?> GetByIdAsync(int courseId, int id, CancellationToken ct);
    Task<IReadOnlyList<EnrollmentResponseDto>> GetByCourseAsync(int courseId, CancellationToken ct);
    Task<EnrollmentResponseDto> CreateAsync(int courseId, EnrollStudentRequest request, CancellationToken ct);

    /// <summary>Returns true if the student is already enrolled in a course with the given code.</summary>
    Task<bool> ExistsAsync(int studentId, string courseCode, CancellationToken ct);

    /// <summary>Persists a new Enrollment entity (used by CQRS handlers).</summary>
    Task AddAsync(Enrollment enrollment, CancellationToken ct);

    /// <summary>Returns all enrollments for a student, including the Course navigation property.</summary>
    Task<IReadOnlyList<Enrollment>> GetByStudentIdAsync(int studentId, CancellationToken ct);
}
