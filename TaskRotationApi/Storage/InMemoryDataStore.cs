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

    public InMemoryDataStore()
    {
        SeedInitialData();
    }

    public T Read<T>(Func<IReadOnlyList<User>, IReadOnlyList<TaskItem>, T> reader)
    {
        lock (_sync)
        {
            var usersCopy = _users.ToList();
            var tasksCopy = _tasks.ToList();
            return reader(usersCopy, tasksCopy);
        }
    }

    public void Write(Action<List<User>, List<TaskItem>> writer)
    {
        lock (_sync)
        {
            writer(_users, _tasks);
        }
    }

    public T Write<T>(Func<List<User>, List<TaskItem>, T> writer)
    {
        lock (_sync)
        {
            return writer(_users, _tasks);
        }
    }

    private void SeedInitialData()
    {
        if (_users.Count != 0 || _tasks.Count != 0)
        {
            return;
        }

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
        {
            _users.Add(new User
            {
                Name = name
            });
        }

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
        {
            _tasks.Add(new TaskItem
            {
                Title = title
            });
        }
    }
}