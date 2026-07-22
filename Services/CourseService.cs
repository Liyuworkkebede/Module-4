using Microsoft.EntityFrameworkCore;
using TmsApi.Data;
using TmsApi.Dtos;
using TmsApi.Entities;

namespace TmsApi.Services;

public class CourseService : ICourseService
{
    private readonly TmsDbContext _context;
    private readonly ILogger<CourseService> _logger;

    public CourseService(TmsDbContext context, ILogger<CourseService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<CourseResponseDto?> GetByIdAsync(int id, CancellationToken ct)
    {
        return await _context.Courses
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CourseResponseDto(
                c.Id,
                c.Code,
                c.Title,
                c.MaxCapacity,
                c.Enrollments.Count
            ))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<CourseResponseDto> CreateAsync(CreateCourseRequest request, CancellationToken ct)
    {
        var course = new Course
        {
            Code = request.Code,
            Title = request.Title,
            MaxCapacity = request.MaxCapacity
        };

        _context.Courses.Add(course);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Created course {CourseId} ({Code})", course.Id, course.Code);

        return (await GetByIdAsync(course.Id, ct))!;
    }

    
    public async Task<bool> CodeExistsAsync(string code, CancellationToken ct)
    {
        return await _context.Courses
            .AnyAsync(c => c.Code == code, ct);
    }

    public async Task<PagedResponse<CourseResponseDto>> GetCoursesAsync(PagedRequest request, CancellationToken ct)
    {
        // 1. Start with a no-tracking IQueryable<Course>
        IQueryable<Course> query = _context.Courses.AsNoTracking();

        // 2. Filter if a search term is provided (case-insensitive)
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            query = query.Where(c => EF.Functions.ILike(c.Title, $"%{request.Search}%")
                                  || EF.Functions.ILike(c.Code, $"%{request.Search}%"));
        }

        // 3. Count before paging
        var totalCount = await query.CountAsync(ct);

        // 4. Apply OrderBy sorting (silently fall back to Title for invalid values)
        var orderBy = request.OrderBy;
        if (orderBy != "Title" && orderBy != "Code" && orderBy != "MaxCapacity")
        {
            orderBy = "Title";
        }

        IQueryable<Course> sortedQuery;
        if (orderBy == "Code")
        {
            sortedQuery = request.Descending
                ? query.OrderByDescending(c => c.Code)
                : query.OrderBy(c => c.Code);
        }
        else if (orderBy == "MaxCapacity")
        {
            sortedQuery = request.Descending
                ? query.OrderByDescending(c => c.MaxCapacity)
                : query.OrderBy(c => c.MaxCapacity);
        }
        else
        {
            sortedQuery = request.Descending
                ? query.OrderByDescending(c => c.Title)
                : query.OrderBy(c => c.Title);
        }

        // 5. Skip/Take paging, project to CourseResponseDto, and materialize
        var items = await sortedQuery
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => new CourseResponseDto(
                c.Id, 
                c.Code, 
                c.Title, 
                c.MaxCapacity, 
                c.Enrollments.Count
            ))
            .ToListAsync(ct);

        // 6. Return new PagedResponse<CourseResponseDto>
        return new PagedResponse<CourseResponseDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}