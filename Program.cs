using Scalar.AspNetCore;
using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = true;
    options.ValidateOnBuild = true;
});

// ProblemDetails service (Exercise 6)
builder.Services.AddProblemDetails();

// CORRECT: EnrollmentService must be SCOPED, not Singleton
builder.Services.AddSingleton<EnrollmentWorker>();
builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();

builder.Services.AddOptions<PaymentOptions>()
    .BindConfiguration("Payments")
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddControllers();

// ✅ CORRECT: Add OpenApi services
builder.Services.AddOpenApi();  // Now this should work after installing Microsoft.AspNetCore.OpenApi

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
    throw new Exception("ይህ ሆን ተብሎ የተፈጠረ ስህተት ነው!");
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
    app.MapOpenApi();  // Now this should work
    app.MapScalarApiReference();  // Now this should work
}

app.Run();