using CORE.Services.IServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace API.Services
{
    public class RefreshTokenCleanupBackgroundService : BackgroundService
    {
        private readonly ILogger<RefreshTokenCleanupBackgroundService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private TimeSpan _cleanupInterval;

        public RefreshTokenCleanupBackgroundService(
            ILogger<RefreshTokenCleanupBackgroundService> logger,
            IServiceProvider serviceProvider,
            IConfiguration configuration)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _configuration = configuration;
            
            // Get cleanup interval from configuration, default to 24 hours if not specified
            var intervalHours = _configuration.GetValue<int?>("RefreshTokenCleanup:IntervalHours") ?? 24;
            _cleanupInterval = TimeSpan.FromHours(intervalHours);
            
            _logger.LogInformation(
                "RefreshTokenCleanupBackgroundService initialized with cleanup interval of {Hours} hours",
                intervalHours);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("RefreshTokenCleanupBackgroundService is starting");

            // Optional: Wait for initial delay before first cleanup
            var initialDelayMinutes = _configuration.GetValue<int?>("RefreshTokenCleanup:InitialDelayMinutes") ?? 5;
            if (initialDelayMinutes > 0)
            {
                _logger.LogInformation(
                    "Waiting {Minutes} minutes before first cleanup run",
                    initialDelayMinutes);
                
                await Task.Delay(TimeSpan.FromMinutes(initialDelayMinutes), stoppingToken);
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Starting scheduled refresh token cleanup");

                    // Create a scope for the scoped service
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var cleanupService = scope.ServiceProvider
                            .GetRequiredService<IRefreshTokenCleanupService>();

                        var deletedCount = await cleanupService.DeleteInactiveRefreshTokensAsync();

                        _logger.LogInformation(
                            "Scheduled cleanup completed. Deleted {Count} inactive refresh tokens",
                            deletedCount);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Error occurred during scheduled refresh token cleanup");
                }

                // Wait for the next cleanup interval
                _logger.LogDebug(
                    "Next refresh token cleanup scheduled in {Hours} hours",
                    _cleanupInterval.TotalHours);

                await Task.Delay(_cleanupInterval, stoppingToken);
            }

            _logger.LogInformation("RefreshTokenCleanupBackgroundService is stopping");
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("RefreshTokenCleanupBackgroundService stop requested");
            await base.StopAsync(cancellationToken);
        }
    }
}
