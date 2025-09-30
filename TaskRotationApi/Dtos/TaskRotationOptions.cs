namespace TaskRotationApi.Dtos;

/// <summary>
///     Configuration options governing the automated task rotation interval.
/// </summary>
public sealed class TaskRotationOptions
{
    /// <summary>
    ///     Gets or sets the number of seconds between automatic task rotations.
    /// </summary>
    public int IntervalSeconds { get; set; } = 120;
}

