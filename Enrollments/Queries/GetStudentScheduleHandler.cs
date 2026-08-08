using MediatR;
using TmsApi.Services;

namespace TmsApi.Enrollments.Queries;

public class GetStudentScheduleHandler(IEnrollmentService enrollmentService)
    : IRequestHandler<GetStudentScheduleQuery, ScheduleDto>
{
    public async Task<ScheduleDto> Handle(
        GetStudentScheduleQuery query, CancellationToken ct)
    {
        var enrollments = await enrollmentService.GetByStudentIdAsync(query.StudentId, ct);

        var items = enrollments
            .Select(e => new ScheduleItemDto(
                e.Course.Code,
                e.Course.Title,
                "TBD"))
            .ToList();

        return new ScheduleDto(query.StudentId, items);
    }
}
