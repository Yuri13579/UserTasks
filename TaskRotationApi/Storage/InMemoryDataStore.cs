using TaskRotationApi.Models;

namespace TaskRotationApi.Storage;

/// <summary>
/// Small helper that keeps all data in memory and protects it with a lock so the
/// API controllers and background scheduler do not step on each other.
/// </summary>
public class InMemoryDataStore
{    
    private readonly object _sync = new();
    private readonly List<User> _users = [];
    private readonly List<TaskItem> _tasks = [];

    public T Read<T>(Func<IReadOnlyList<User>, IReadOnlyList<TaskItem>, T> reader)
    {
        lock (_sync)
        {
            return reader(_users, _tasks);
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
}
