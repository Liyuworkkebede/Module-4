using Microsoft.AspNetCore.Mvc;

namespace TmsApi.Controllers;

[ApiController]
[Route("api/enrollments")]
public class EnrollmentsController : ControllerBase
{
    private readonly IEnrollmentService _enrollmentService;

    public EnrollmentsController(IEnrollmentService enrollmentService)
    {
        _enrollmentService = enrollmentService;
    }

    // GET /api/enrollments - returns all enrollment records
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var enrollments = await _enrollmentService.GetAllAsync();
        return Ok(enrollments);
    }

    // GET /api/enrollments/{id} - returns one or 404
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var record = await _enrollmentService.GetByIdAsync(id);
        return record is not null ? Ok(record) : NotFound();
    }

    // POST /api/enrollments - creates and returns 201 with Location header
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEnrollmentRequest request)
    {
        var record = await _enrollmentService.EnrollAsync(request.StudentId, request.CourseCode);
        return CreatedAtAction(nameof(GetById), new { id = record.Id }, record);
    }

    // DELETE /api/enrollments/{id} - returns 204 or 404
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var deleted = await _enrollmentService.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}

// Request model (can be in same file or separate Models folder)
public record CreateEnrollmentRequest(string StudentId, string CourseCode);