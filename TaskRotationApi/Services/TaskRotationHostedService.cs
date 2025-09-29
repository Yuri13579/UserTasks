using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TaskRotationApi.Services
{
    /// <summary>
    /// Periodically rotates the task assignments to fulfil the scheduling rules.
    /// </summary>
    public class TaskRotationHostedService : BackgroundService
    {
        private readonly TaskAssignmentService _service;
        private readonly ILogger<TaskRotationHostedService> _logger;
        private readonly TimeSpan _interval;

        public TaskRotationHostedService(TaskAssignmentService service, ILogger<TaskRotationHostedService> logger, IConfiguration configuration)
        {
            _service = service;
            _logger = logger;

            var seconds = configuration.GetValue("TaskRotation:IntervalSeconds", 120);
            if (seconds < 5)
            {
                seconds = 5; // guard against accidentally disabling the loop.
            }

            _interval = TimeSpan.FromSeconds(seconds);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Task rotation service started. Interval: {Interval}", _interval);

            // Run once at startup so waiting tasks can be picked up immediately.
            PerformRotationSafely();

            try
            {
                using var timer = new PeriodicTimer(_interval);
                while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
                {
                    PerformRotationSafely();
                }
            }
            catch (OperationCanceledException)
            {
                // graceful shutdown
            }
        }

        private void PerformRotationSafely()
        {
            try
            {
                _service.RotateAssignments();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to rotate task assignments");
            }
        }
    }
}
