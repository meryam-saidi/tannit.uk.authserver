using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Tannit.AuthServer.Repositories;

public class ExpiredCodeCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ExpiredCodeCleanupService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromHours(1); // Change if needed

    public ExpiredCodeCleanupService(
        IServiceProvider serviceProvider,
        ILogger<ExpiredCodeCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<IEmailVerificationRepository>();

                await repository.DeleteExpiredCodesAsync();
                _logger.LogInformation("✅ Expired verification codes cleaned up at {Time}", DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error during cleanup of expired email verification codes");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }
}
