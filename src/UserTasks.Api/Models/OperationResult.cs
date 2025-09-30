using System;

namespace UserTasks.Api.Models;

public class OperationResult<T>
{
    private OperationResult(bool success, T? value, string? error)
    {
        Success = success;
        Value = value;
        Error = error;
    }

    public bool Success { get; }

    public T? Value { get; }

    public string? Error { get; }

    public static OperationResult<T> Successful(T value)
    {
        return new OperationResult<T>(true, value, null);
    }

    public static OperationResult<T> Failure(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("An error message is required.", nameof(message));
        }

        return new OperationResult<T>(false, default, message);
    }
}
