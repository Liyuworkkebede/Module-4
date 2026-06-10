using Microsoft.AspNetCore.Authentication;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOptions<PaymentOptions>()
    .BindConfiguration("Payments")
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddSingleton<EnrollmentWorker>();
builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();

// Services: add authentication / authorization services
builder.Services.AddAuthentication("Training")
    .AddScheme<AuthenticationSchemeOptions, TrainingAuthHandler>("Training", null);

builder.Services.AddAuthorization();

var app = builder.Build();
app.UseMiddleware<RequestLoggingMiddleware>();

app.UseExceptionHandler("/error");

// TODO 1: Register routing in the pipeline where it belongs for your app.
app.UseRouting();

// TODO 2: Register authentication and authorization in the pipeline where your template and facilitator expect them for a protected minimal API route.
app.UseAuthentication();
app.UseAuthorization();

// TODO 3: Map GET /api/assessments/results with the same response body as the starter, but require authorization for that route.
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

app.Run();