using Asp.Versioning;
using Scalar.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using MediatR;
using TmsApi.Behaviors;
using TmsApi.Data;
using TmsApi.Entities;
using TmsApi.Enrollments.Commands;
using TmsApi.ExceptionHandlers;
using TmsApi.Filters;
using TmsApi.Middleware;
using TmsApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes  = true;
    options.ValidateOnBuild = true;
});

// ========== DATABASE ==========
builder.Services.AddDbContext<TmsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("TmsDatabase"))
           .LogTo(Console.WriteLine, LogLevel.Information)
           .EnableSensitiveDataLogging());

// ========== PROBLEM DETAILS ==========
builder.Services.AddProblemDetails();

// ========== EXCEPTION HANDLER ==========
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// ========== SERVICE REGISTRATIONS ==========
builder.Services.AddSingleton<EnrollmentWorker>();
builder.Services.AddScoped<ICourseService,    CourseService>();
builder.Services.AddScoped<IEnrollmentService, TmsApi.Services.EnrollmentService>();

// ========== MEDIATR ==========
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(EnrollStudentHandler).Assembly));

// ========== FLUENT VALIDATION ==========
builder.Services.AddValidatorsFromAssembly(typeof(EnrollStudentValidator).Assembly);

// ========== PIPELINE BEHAVIORS (Logging FIRST so it wraps Validation) ==========
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

// ========== OPTIONS ==========
builder.Services.AddOptions<PaymentOptions>()
    .BindConfiguration("Payments")
    .ValidateDataAnnotations()
    .ValidateOnStart();

// ========== CONTROLLERS ==========
builder.Services.AddControllers(options =>
{
    options.Filters.Add<AuditLogFilter>();
});

// ========== API VERSIONING ==========
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion                   = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions                   = true;
    options.ApiVersionReader                    = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),
        new HeaderApiVersionReader("X-Api-Version"));
})
.AddMvc()
.AddApiExplorer(options =>
{
    options.GroupNameFormat           = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

// ========== OPENAPI (one document per version) ==========
builder.Services.AddOpenApi("v1", options =>
{
    options.ShouldInclude = description => description.GroupName == "v1";
});
builder.Services.AddOpenApi("v2", options =>
{
    options.ShouldInclude = description => description.GroupName == "v2";
});

// ========== AUTHENTICATION ==========
builder.Services.AddAuthentication("Training")
    .AddScheme<AuthenticationSchemeOptions, TrainingAuthHandler>("Training", null);
builder.Services.AddAuthorization();

// ====================================================================
var app = builder.Build();
// ====================================================================

// Exception handler FIRST — before routing so all exceptions are caught
app.UseExceptionHandler();

app.UseMiddleware<RequestLoggingMiddleware>();

// V1 deprecation headers stamped before the response body is written
app.UseMiddleware<V1DeprecationMiddleware>();

app.UseStatusCodePages();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// ========== MINIMAL API ROUTES ==========
app.MapGet("/api/error", () =>
{
    throw new Exception("This is an intentionally created error!");
});

app.MapGet("/api/assessments/results", () =>
    Results.Ok(new { courseCode = "CS-101", studentId = "S-001", letterGrade = "A" }))
   .RequireAuthorization();

app.MapGet("/api/enrollments/worker-smoke", async (EnrollmentWorker worker) =>
{
    await worker.ProcessBatchAsync();
    return Results.Ok("processed");
});

// ========== SCALAR + OPENAPI (development only) ==========
if (app.Environment.IsDevelopment())
{
    // Registers /openapi/v1.json and /openapi/v2.json (one doc per AddOpenApi call above)
    app.MapOpenApi();

    app.MapScalarApiReference(options =>
    {
        options.WithTitle("TMS API Reference")
               .WithTheme(ScalarTheme.DeepSpace)
               .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);

        // Sidebar dropdown shows both versions
        options.AddDocument("v1", "API Version 1.0")
               .AddDocument("v2", "API Version 2.0");
    });
}

// ========== DATABASE SEEDING ==========
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<TmsDbContext>();
    await context.Database.MigrateAsync();

    if (!await context.Students.AnyAsync())
    {
        var students = new List<Student>
        {
            new() { RegistrationNumber = "TMS-2026-0001", Name = "Alice Smith",   GPA = 3.8m, IsActive = true  },
            new() { RegistrationNumber = "TMS-2026-0002", Name = "Bob Jones",     GPA = 2.9m, IsActive = true  },
            new() { RegistrationNumber = "TMS-2026-0003", Name = "Charlie Brown", GPA = 3.4m, IsActive = false },
            new() { RegistrationNumber = "TMS-2026-0004", Name = "Diana Prince",  GPA = 3.9m, IsActive = true  },
            new() { RegistrationNumber = "TMS-2026-0005", Name = "Evan Wright",   GPA = 2.5m, IsActive = true  }
        };
        await context.Students.AddRangeAsync(students);
        await context.SaveChangesAsync();

        var courses = new List<Course>
        {
            new() { Code = "CS-101",  Title = "Introduction to Computer Science", MaxCapacity = 30 },
            new() { Code = "CS-201",  Title = "Data Structures and Algorithms",   MaxCapacity = 25 },
            new() { Code = "MAT-101", Title = "Calculus I",                       MaxCapacity = 40 }
        };
        await context.Courses.AddRangeAsync(courses);
        await context.SaveChangesAsync();

        var savedStudents = await context.Students.ToListAsync();
        var savedCourses  = await context.Courses.ToListAsync();

        var enrollments = new List<Enrollment>
        {
            new() { StudentId = savedStudents[0].Id, CourseId = savedCourses[0].Id, Grade = 4.0m },
            new() { StudentId = savedStudents[0].Id, CourseId = savedCourses[1].Id, Grade = 3.6m },
            new() { StudentId = savedStudents[1].Id, CourseId = savedCourses[0].Id, Grade = 2.8m },
            new() { StudentId = savedStudents[3].Id, CourseId = savedCourses[1].Id, Grade = 3.9m }
        };
        await context.Enrollments.AddRangeAsync(enrollments);
        await context.SaveChangesAsync();
    }
}

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<TmsDbContext>();
    await DataSeeder.SeedAsync(context);
}

app.Run();
