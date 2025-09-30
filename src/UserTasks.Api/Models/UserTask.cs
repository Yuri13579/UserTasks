namespace UserTasks.Api.Models;

public record UserTask(Guid Id, string Title, string Description, DateTime DueDate, bool IsCompleted);
