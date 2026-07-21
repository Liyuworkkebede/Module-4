using Scalar.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using TmsApi.Data;
using TmsApi.Entities;
using TmsApi.Services; 
using TmsApi.Dtos;     

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = true;
    options.ValidateOnBuild = true;
});

// ========== DATABASE CONFIGURATION ==========
builder.Services.AddDbContext<TmsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("TmsDatabase"))
        .LogTo(Console.WriteLine, LogLevel.Information)
        .EnableSensitiveDataLogging());

// ========== PROBLEM DETAILS ==========
builder.Services.AddProblemDetails();

// ========== SERVICE REGISTRATIONS ==========
builder.Services.AddSingleton<EnrollmentWorker>();
builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();

// ✅ ADD COURSE SERVICE
builder.Services.AddScoped<ICourseService, CourseService>();
builder.Services.AddScoped<TmsApi.Services.IEnrollmentService, TmsApi.Services.EnrollmentService>();

// ========== OPTIONS ==========
builder.Services.AddOptions<PaymentOptions>()
    .BindConfiguration("Payments")
    .ValidateDataAnnotations()
    .ValidateOnStart();

// ========== CONTROLLERS ==========
builder.Services.AddControllers();

// ========== OPENAPI ==========
builder.Services.AddOpenApi();

// ========== AUTHENTICATION ==========
builder.Services.AddAuthentication("Training")
    .AddScheme<AuthenticationSchemeOptions, TrainingAuthHandler>("Training", null);

builder.Services.AddAuthorization();


var app = builder.Build();

app.UseMiddleware<RequestLoggingMiddleware>();

// Exception Handler and StatusCodePages (Exercise 6)
app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Map a test route '/api/error' (Exercise 6)
app.MapGet("/api/error", () =>
{
   throw new Exception("This is an intentionally created error!");
});

app.MapGet("/api/assessments/results", () =>
{
    return Results.Ok(new
    {
        courseCode = "CS-101",
        studentId = "S-001",
        letterGrade = "A"
    });
})
.RequireAuthorization();

app.MapGet("/api/enrollments/worker-smoke", async (EnrollmentWorker worker) =>
{
    await worker.ProcessBatchAsync();
    return Results.Ok("processed");
});

// Environment-aware configuration (Exercise 7)
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// ========== DATABASE SEEDING ==========
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<TmsDbContext>();
    
    // Apply any pending migrations
    await context.Database.MigrateAsync();
    
    // Seed data if database is empty
    if (!await context.Students.AnyAsync())
    {
        // Create students
        var students = new List<Student>
        {
            new() { RegistrationNumber = "TMS-2026-0001", Name = "Alice Smith", GPA = 3.8m, IsActive = true },
            new() { RegistrationNumber = "TMS-2026-0002", Name = "Bob Jones", GPA = 2.9m, IsActive = true },
            new() { RegistrationNumber = "TMS-2026-0003", Name = "Charlie Brown", GPA = 3.4m, IsActive = false },
            new() { RegistrationNumber = "TMS-2026-0004", Name = "Diana Prince", GPA = 3.9m, IsActive = true },
            new() { RegistrationNumber = "TMS-2026-0005", Name = "Evan Wright", GPA = 2.5m, IsActive = true }
        };
        await context.Students.AddRangeAsync(students);
        await context.SaveChangesAsync();

        // Create courses
        var courses = new List<Course>
{
    new() { Code = "CS-101", Title = "Introduction to Computer Science", MaxCapacity = 30 },
    new() { Code = "CS-201", Title = "Data Structures and Algorithms", MaxCapacity = 25 },
    new() { Code = "MAT-101", Title = "Calculus I", MaxCapacity = 40 }
};
        await context.Courses.AddRangeAsync(courses);
        await context.SaveChangesAsync();

        // Get the saved entities with their generated IDs
        var savedStudents = await context.Students.ToListAsync();
        var savedCourses = await context.Courses.ToListAsync();

        // Create enrollments using the saved entities
        var enrollments = new List<Enrollment>
        {
            new() { StudentId = savedStudents[0].Id, CourseId = savedCourses[0].Id, Grade = 4.0m },
            new() { StudentId = savedStudents[0].Id, CourseId = savedCourses[1].Id, Grade = 3.6m },
            new() { StudentId = savedStudents[1].Id, CourseId = savedCourses[0].Id, Grade = 2.8m },
            new() { StudentId = savedStudents[3].Id, CourseId = savedCourses[1].Id, Grade = 3.9m }
        };
        await context.Enrollments.AddRangeAsync(enrollments);
        await context.SaveChangesAsync();
        
        Console.WriteLine("✅ Database seeded with test data!");
    }
}

app.Run();