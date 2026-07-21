using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Data;
using TmsApi.Entities;

namespace TmsApi.Controllers;

[ApiController]
[Route("api/performance")]
public class PerformanceController : ControllerBase
{
    private readonly TmsDbContext _context;
    private readonly ILogger<PerformanceController> _logger;

    public PerformanceController(TmsDbContext context, ILogger<PerformanceController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ========== PART A: INTENTIONAL N+1 (BAD) ==========
    
    [HttpGet("n-plus-one")]
    public async Task<IActionResult> TestNPlusOne()
    {
        Console.WriteLine("\n=== N+1 QUERY DEMONSTRATION (BAD) ===");
        
        // 1 query to get all students
        var students = await _context.Students
            .AsNoTracking()
            .ToListAsync();

        var results = new List<object>();
        int queryCount = 1; // We already did 1 query

        // N queries inside the loop (one per student)
        foreach (var s in students)
        {
            // ⚠️ This runs a separate query for EACH student!
            var count = await _context.Enrollments
                .AsNoTracking()
                .CountAsync(e => e.StudentId == s.Id);
            
            queryCount++;
            results.Add(new { s.Name, EnrollmentCount = count });
            Console.WriteLine($"{s.Name}: {count} enrollments (query {queryCount})");
        }

        Console.WriteLine($"\n📊 Total queries executed: {queryCount} (1 + {students.Count} = N+1)");
        
        return Ok(new
        {
            Message = $"N+1 query executed: {queryCount} queries",
            Data = results
        });
    }

    // ========== PART B: FIXED WITH PROJECTION (GOOD) ==========
    
    [HttpGet("n-plus-one-fixed")]
    public async Task<IActionResult> TestNPlusOneFixed()
    {
        Console.WriteLine("\n=== N+1 FIXED WITH PROJECTION (GOOD) ===");
        
        // ✅ Single query with projection
        var report = await _context.Students
            .AsNoTracking()
            .Select(s => new
            {
                s.Name,
                EnrollmentCount = s.Enrollments.Count  // Translates to SQL subquery!
            })
            .ToListAsync();

        Console.WriteLine($"📊 Total queries executed: 1 (single query with subquery)");
        
        foreach (var r in report)
        {
            Console.WriteLine($"{r.Name}: {r.EnrollmentCount} enrollments");
        }

        return Ok(new
        {
            Message = $"Single query with projection executed: 1 query",
            Data = report
        });
    }

    // ========== PART C: FIXED WITH INCLUDE ==========
    
    [HttpGet("include-fix")]
    public async Task<IActionResult> TestWithInclude()
    {
        Console.WriteLine("\n=== FIXED WITH INCLUDE ===");
        
        // ✅ Single query with Include to load related data
        var students = await _context.Students
            .AsNoTracking()
            .Include(s => s.Enrollments)
            .ToListAsync();

        Console.WriteLine($"📊 Total queries executed: 1 (with JOIN)");

        foreach (var s in students)
        {
            Console.WriteLine($"{s.Name}: {s.Enrollments.Count} enrollments");
        }

        return Ok(new
        {
            Message = $"Single query with Include executed: 1 query",
            Data = students.Select(s => new { s.Name, EnrollmentCount = s.Enrollments.Count })
        });
    }
}