namespace TaskRotationApi.Models;

/// <summary>
///     Enumerates the possible lifecycle states of a task.
/// </summary>
public enum TaskState
{
    /// <summary>
    ///     The task is waiting for an available user.
    /// </summary>
    Waiting = 0,

    /// <summary>
    ///     The task is currently assigned to a user.
    /// </summary>
    InProgress = 1,

    /// <summary>
    ///     The task has completed its rotation and no longer needs processing.
    /// </summary>
    Completed = 2
}
