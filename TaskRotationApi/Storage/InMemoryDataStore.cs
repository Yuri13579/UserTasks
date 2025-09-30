using TaskRotationApi.Models;

namespace TaskRotationApi.Storage;

/// <summary>
///     Small helper that keeps all data in memory and protects it with a lock so the
///     API controllers and background scheduler do not step on each other.
/// </summary>
public class InMemoryDataStore
{
    private readonly object _sync = new();
    private readonly List<TaskItem> _tasks = [];
    private readonly List<User> _users = [];

    /// <summary>
    ///     Reads data from the store using a delegate while holding a lock.
    /// </summary>
    /// <typeparam name="T">The type returned by the delegate.</typeparam>
    /// <param name="reader">The function that consumes copies of the current users and tasks.</param>
    /// <returns>The value returned by the <paramref name="reader"/> delegate.</returns>
    public T Read<T>(Func<IReadOnlyList<User>, IReadOnlyList<TaskItem>, T> reader)
    {
        lock (_sync)
        {
            var usersCopy = _users.ToList();
            var tasksCopy = _tasks.ToList();
            return reader(usersCopy, tasksCopy);
        }
    }

    /// <summary>
    ///     Executes a write action under a lock to mutate the underlying collections.
    /// </summary>
    /// <param name="writer">The action that performs updates on the shared collections.</param>
    public void Write(Action<List<User>, List<TaskItem>> writer)
    {
        lock (_sync)
        {
            writer(_users, _tasks);
        }
    }

    /// <summary>
    ///     Executes a write function under a lock and returns its result.
    /// </summary>
    /// <typeparam name="T">The type returned by the <paramref name="writer"/> delegate.</typeparam>
    /// <param name="writer">The function that performs updates on the shared collections.</param>
    /// <returns>The value produced by the <paramref name="writer"/> delegate.</returns>
    public T Write<T>(Func<List<User>, List<TaskItem>, T> writer)
    {
        lock (_sync)
        {
            return writer(_users, _tasks);
        }
    }

    /// <summary>
    ///     Seeds the store with a predefined set of users and tasks if empty.
    /// </summary>
    /// <returns><c>true</c> if data was seeded; otherwise, <c>false</c>.</returns>
    public bool SeedInitialData()
    {
        lock (_sync)
        {
            if (_users.Count != 0 || _tasks.Count != 0) return false;

            string[] defaultUsers =
            {
                "Liam",
                "Noah",
                "Oliver",
                "Theodore",
                "James",
                "Henry",
                "Mateo",
                "Elijah",
                "Lucas",
                "William"
            };

            foreach (var name in defaultUsers)
                _users.Add(new User
                {
                    Name = name
                });

            string[] defaultTasks =
            {
                "Ride",
                "Sit down",
                "Win",
                "Drink",
                "Knit",
                "Stand",
                "Throw",
                "Close",
                "Open",
                "Skip",
                "Sleep",
                "Cut",
                "Eat",
                "Cook",
                "Sip",
                "Fight",
                "Play",
                "Give",
                "Dig",
                "Bath"
            };

            foreach (var title in defaultTasks)
                _tasks.Add(new TaskItem
                {
                    Title = title
                });

            return true;
        }
    }
}
