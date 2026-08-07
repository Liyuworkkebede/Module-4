using Microsoft.Extensions.DependencyInjection;
using TmsApi.Services;

public class EnrollmentWorker
{
    private readonly IServiceScopeFactory _scopeFactory;

    public EnrollmentWorker(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task ProcessBatchAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var svc = scope.ServiceProvider
            .GetRequiredService<IEnrollmentService>();

        // Placeholder batch processing — no-op for now
        await Task.CompletedTask;
        Console.WriteLine("EnrollmentWorker batch processed.");
    }
}
