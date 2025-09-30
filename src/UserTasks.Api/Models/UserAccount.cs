using System;

namespace UserTasks.Api.Models;

public class UserAccount
{
    public UserAccount(Guid id, string name, bool isAvailable = true)
    {
        Id = id;
        Name = name;
        IsAvailable = isAvailable;
    }

    public Guid Id { get; }

    public string Name { get; }

    public bool IsAvailable { get; private set; }

    public void MarkBusy()
    {
        IsAvailable = false;
    }

    public void MarkAvailable()
    {
        IsAvailable = true;
    }
}
