namespace TaskRotationApi.Services;

/// <summary>
///     Periodically rotates the task assignments to fulfil the scheduling rules.
/// </summary>
public class TaskRotationHostedService : BackgroundService
{
    private readonly TimeSpan _interval;
    private readonly ILogger<TaskRotationHostedService> _logger;
    private readonly TaskAssignmentService _service;

    /// <summary>
    ///     Initializes a new instance of the <see cref="TaskRotationHostedService"/> class.
    /// </summary>
    /// <param name="service">The domain service used to rotate tasks.</param>
    /// <param name="logger">Logger used to emit diagnostic messages.</param>
    /// <param name="configuration">Configuration source for rotation options.</param>
    public TaskRotationHostedService(TaskAssignmentService service, ILogger<TaskRotationHostedService> logger,
        IConfiguration configuration)
    {
        _service = service;
        _logger = logger;

        var seconds = configuration.GetValue<int?>("TaskRotation:IntervalSeconds") ?? 120;
        if (seconds <= 0) seconds = 120;
        if (seconds < 5) seconds = 5;

        _interval = TimeSpan.FromSeconds(seconds);
    }

    /// <summary>
    ///     Executes the background rotation loop until cancellation is requested.
    /// </summary>
    /// <param name="stoppingToken">Token signalling when the host is shutting down.</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Task rotation service started. Interval: {Interval}", _interval);

        // Run once at startup so waiting tasks can be picked up immediately.
        SafeRotate();

        try
        {
            using var timer = new PeriodicTimer(_interval);
            while (await timer.WaitForNextTickAsync(stoppingToken)) SafeRotate();
        }
        catch (OperationCanceledException)
        {
            // graceful shutdown
        }
    }

    /// <summary>
    ///     Safely executes a rotation cycle while handling unexpected exceptions.
    /// </summary>
    private void SafeRotate()
    {
        try
        {
            var changes = _service.RotateAssignments();
            foreach (var change in changes)
                _logger.LogInformation(
                    "Task {TaskId} reassigned from {FromUserId} to {ToUserId}",
                    change.TaskId,
                    change.FromUserId,
                    change.ToUserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rotate task assignments");
        }
    }
}