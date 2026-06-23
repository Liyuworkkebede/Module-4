using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Data;
using TmsApi.Entities;

namespace TmsApi.Controllers;

[ApiController]
[Route("api/test")]
public class TestController : ControllerBase
{
    private readonly TmsDbContext _context;
    private readonly ILogger<TestController> _logger;

    public TestController(TmsDbContext context, ILogger<TestController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ========== EXERCISE 2: DEFERRED EXECUTION EXPERIMENT ==========
    [HttpGet("deferred")]
    public async Task<IActionResult> TestDeferred()
    {
        Console.WriteLine("\n>> STEP 1: Building the query object (no database contact)...");
        var query = _context.Students.Where(s => s.GPA >= 3.0m);

        Console.WriteLine("\n>> STEP 2: Appending a sorting clause...");
        var orderedQuery = query.OrderBy(s => s.Name);

        Console.WriteLine(">> STEP 3: Materializing query into a C# List...");
        var results = await orderedQuery.ToListAsync();  // ⚡ SQL executes HERE!

        Console.WriteLine(">> STEP 4: Materialization finished. List populated.\n");
        
        _logger.LogInformation("Returning {Count} students", results.Count);
        return Ok(results);
    }

    // ========== EXERCISE 2: TRANSLATION FAILURE EXPERIMENT ==========
    // Non-translatable helper method
    private static bool IsHonorRoll(decimal gpa)
    {
        return gpa >= 3.5m;
    }

    [HttpGet("translation-fail")]
    public async Task<IActionResult> TestTranslationFail()
    {
        Console.WriteLine("\n>> STEP 1: Running non-translatable query...");
        try
        {
            // ❌ This will FAIL - EF Core can't translate custom C# methods
            var students = await _context.Students
                .Where(s => IsHonorRoll(s.GPA))  // Custom method can't be translated
                .ToListAsync();
            return Ok(students);
        }
        catch (Exception ex)
        {
            Console.WriteLine($">>> EXCEPTION CAUGHT: {ex.Message}\n");
            return BadRequest(new { Message = ex.Message });
        }
    }

    // ========== EXERCISE 2: TRANSLATION FIXED ==========
    [HttpGet("translation-fixed")]
    public async Task<IActionResult> TestTranslationFixed()
    {
        Console.WriteLine("\n>> Running translatable query...");
        
        // ✅ FIXED: Inline expression that EF can translate
        var students = await _context.Students
            .Where(s => s.GPA >= 3.5m)  // ✅ Translates to SQL
            .ToListAsync();
        
        _logger.LogInformation("Found {Count} honor roll students", students.Count);
        return Ok(students);
    }

    // ========== EXERCISE 2: CLIENT-SIDE EVALUATION (Bad Practice) ==========
    [HttpGet("translation-client")]
    public async Task<IActionResult> TestTranslationClient()
    {
        Console.WriteLine("\n>> Running client-side evaluation (BAD PRACTICE)...");
        
        // ❌ BAD: Pulls ALL rows into memory first
        var students = _context.Students
            .AsEnumerable()  // ⚠️ Executes query immediately, loads ALL data
            .Where(s => IsHonorRoll(s.GPA))  // Filtering happens in C# memory
            .ToList();
        
        _logger.LogWarning("Client-side evaluation used! Loaded all students into memory.");
        return Ok(students);
    }

    // ========== EXERCISE 2: REGISTRAR'S BUSINESS QUERIES ==========
    [HttpGet("registrar-queries")]
    public async Task<IActionResult> RegistrarQueries()
    {
        var results = new Dictionary<string, object>();

        // 1. How many active students have GPA >= 3.0?
        Console.WriteLine("\n>> Query 1: Active students with GPA >= 3.0");
        results["ActiveStudentsGpa3Plus"] = await _context.Students
            .Where(s => s.IsActive && s.GPA >= 3.0m)
            .CountAsync();

        // 2. Which courses have the most enrollments, sorted descending?
        Console.WriteLine("\n>> Query 2: Courses with most enrollments");
        results["CourseEnrollmentCounts"] = await _context.Courses
            .Select(c => new { c.Title, EnrollmentCount = c.Enrollments.Count })
            .OrderByDescending(x => x.EnrollmentCount)
            .ToListAsync();

        // 3. What is the average GPA per course?
        Console.WriteLine("\n>> Query 3: Average GPA per course");
        results["AverageGpaPerCourse"] = await _context.Enrollments
            .GroupBy(e => e.Course.Title)
            .Select(g => new { Course = g.Key, AverageGPA = g.Average(e => e.Student.GPA) })
            .ToListAsync();

        // 4. Students with zero enrollments (Approach A - Subquery)
        Console.WriteLine("\n>> Query 4a: Students with no enrollments (Subquery)");
        results["StudentsWithNoEnrollments_Subquery"] = await _context.Students
            .Where(s => !s.Enrollments.Any())
            .Select(s => s.Name)
            .ToListAsync();

        // 5. Students with zero enrollments (Approach B - Left Join)
        Console.WriteLine("\n>> Query 4b: Students with no enrollments (Left Join)");
        results["StudentsWithNoEnrollments_LeftJoin"] = await _context.Students
            .GroupJoin(
                _context.Enrollments,
                student => student.Id,
                enrollment => enrollment.StudentId,
                (student, enrollments) => new { student, enrollments }
            )
            .Where(x => !x.enrollments.Any())
            .Select(x => x.student.Name)
            .ToListAsync();

        return Ok(results);
    }
}