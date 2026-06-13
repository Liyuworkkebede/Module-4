using Microsoft.Extensions.DependencyInjection;

public class EnrollmentWorker
{
    private readonly IServiceScopeFactory _scopeFactory;

    public EnrollmentWorker(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task ProcessBatchAsync()  // ← Must be async Task
    {
        using var scope = _scopeFactory.CreateScope();
        var svc = scope.ServiceProvider
            .GetRequiredService<IEnrollmentService>();

        var enrollments = await svc.GetAllAsync();  // ← Use await, not .Result

        Console.WriteLine($"Processed {enrollments.Count} enrollments");
    }
}