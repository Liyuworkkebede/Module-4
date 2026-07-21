using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Data;
using TmsApi.Entities;

namespace TmsApi.Controllers;

[ApiController]
[Route("api/concurrency")]
public class ConcurrencyController : ControllerBase
{
    private readonly TmsDbContext _context;
    private readonly ILogger<ConcurrencyController> _logger;

    public ConcurrencyController(TmsDbContext context, ILogger<ConcurrencyController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ========== GET STUDENT WITH VERSION ==========
    // GET: /api/concurrency/student/{id}
    [HttpGet("student/{id}")]
    public async Task<IActionResult> GetStudent(int id)
    {
        var student = await _context.Students.FindAsync(id);
        if (student == null)
        {
            return NotFound(new { Message = $"Student with ID {id} not found" });
        }

        return Ok(new
        {
            student.Id,
            student.Name,
            student.GPA,
            student.IsActive,
            student.Version  // ✅ Concurrency token
        });
    }

    // ========== UPDATE STUDENT ==========
    // PUT: /api/concurrency/student/{id}?version={version}
    [HttpPut("student/{id}")]
    public async Task<IActionResult> UpdateStudent(
        int id,
        [FromBody] UpdateStudentRequest request,
        [FromQuery] uint version)  // ✅ Client sends the version
    {
        try
        {
            var student = await _context.Students.FindAsync(id);
            if (student == null)
            {
                return NotFound(new { Message = $"Student with ID {id} not found" });
            }

            // ✅ Concurrency check - compare versions
            if (student.Version != version)
            {
                return Conflict(new
                {
                    Message = "Record was modified by another user. Please refresh and try again.",
                    CurrentVersion = student.Version
                });
            }

            // Update student
            if (!string.IsNullOrEmpty(request.Name))
                student.Name = request.Name;

            if (request.GPA.HasValue)
                student.GPA = request.GPA.Value;

            // ✅ Set shadow property for audit
            _context.Entry(student).Property("LastUpdated").CurrentValue = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Student {id} updated successfully");

            return Ok(new
            {
                Message = "Student updated successfully",
                student.Id,
                student.Name,
                student.GPA,
                student.Version  // ✅ New version
            });
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new
            {
                Message = "Concurrency conflict. Record was modified by another user."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating student {Id}", id);
            return StatusCode(500, new { Message = "An error occurred", Error = ex.Message });
        }
    }
}

// ========== REQUEST DTO ==========
public record UpdateStudentRequest(string? Name, decimal? GPA);