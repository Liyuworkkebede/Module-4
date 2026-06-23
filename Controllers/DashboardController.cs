using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Data;
using TmsApi.Entities;

namespace TmsApi.Controllers;

[ApiController]
[Route("api/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly TmsDbContext _context;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(TmsDbContext context, ILogger<DashboardController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ========== EXERCISE 3: PAGINATION ==========
    
    // GET: /api/dashboard/students?page=1&pageSize=20
    [HttpGet("students")]
    public async Task<IActionResult> GetPagedStudents(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            // Validate parameters
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > 100) pageSize = 100;  // Max page size

            // TODO 1: Pagination - OrderBy, Skip, Take
            // ⚠️ IMPORTANT: Always OrderBy before Skip/Take!
            var query = _context.Students
                .OrderBy(s => s.Name)  // ✅ Stable sort
                .ThenBy(s => s.Id);     // ✅ Tie-breaker

            // Get total count for pagination metadata
            var totalCount = await query.CountAsync();
            
            // Apply pagination
            var students = await query
                .Skip((page - 1) * pageSize)  // Offset
                .Take(pageSize)                // Limit
                .ToListAsync();

            // Build response with metadata
            var response = new
            {
                Data = students,
                Pagination = new
                {
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                    HasPreviousPage = page > 1,
                    HasNextPage = page < (int)Math.Ceiling((double)totalCount / pageSize)
                }
            };

            _logger.LogInformation("Retrieved page {Page} with {Count} students (total: {Total})", 
                page, students.Count, totalCount);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting paged students");
            return StatusCode(500, new { Message = "An error occurred", Error = ex.Message });
        }
    }

    // ========== EXERCISE 3: TOP 5 COURSES ==========
    
    // GET: /api/dashboard/top-courses
    [HttpGet("top-courses")]
    public async Task<IActionResult> GetTopCourses()
    {
        try
        {
            // TODO 2: Top 5 courses by enrollment count
            // ✅ SQL: GROUP BY, ORDER BY COUNT, LIMIT 5
            var topCourses = await _context.Courses
                .Select(c => new
                {
                    c.Id,
                    c.Code,
                    c.Title,
                    EnrollmentCount = c.Enrollments.Count
                })
                .OrderByDescending(c => c.EnrollmentCount)  // Sort by count descending
                .Take(5)  // Only top 5
                .ToListAsync();

            _logger.LogInformation("Retrieved top {Count} courses by enrollment", topCourses.Count);

            return Ok(new
            {
                Data = topCourses,
                Count = topCourses.Count,
                GeneratedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting top courses");
            return StatusCode(500, new { Message = "An error occurred", Error = ex.Message });
        }
    }

    // ========== BONUS: ENROLLMENT SUMMARY ==========
    
    // GET: /api/dashboard/summary
    [HttpGet("summary")]
    public async Task<IActionResult> GetDashboardSummary()
    {
        try
        {
            var summary = new
            {
                TotalStudents = await _context.Students.CountAsync(),
                ActiveStudents = await _context.Students.CountAsync(s => s.IsActive),
                TotalCourses = await _context.Courses.CountAsync(),
                TotalEnrollments = await _context.Enrollments.CountAsync(),
                AverageGPA = await _context.Students.AverageAsync(s => s.GPA),
                TopCourse = await _context.Courses
                    .Select(c => new
                    {
                        c.Title,
                        EnrollmentCount = c.Enrollments.Count
                    })
                    .OrderByDescending(c => c.EnrollmentCount)
                    .FirstOrDefaultAsync(),
                StudentsWithNoEnrollments = await _context.Students
                    .CountAsync(s => !s.Enrollments.Any())
            };

            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard summary");
            return StatusCode(500, new { Message = "An error occurred", Error = ex.Message });
        }
    }
}