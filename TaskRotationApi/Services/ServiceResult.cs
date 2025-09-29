namespace TaskRotationApi.Services;

public enum ErrorCode
{
    None = 0,
    Duplicate,
    NotFound,
    Invalid,
    LimitReached
}

public readonly record struct ServiceResult<T>(bool Success, ErrorCode Code, string? Error, T? Value)
{
    public static ServiceResult<T> SuccessResult(T value) => new(true, ErrorCode.None, null, value);
    public static ServiceResult<T> Failure(ErrorCode code, string message) => new(false, code, message, default);
}

public readonly record struct ServiceResult(bool Success, ErrorCode Code, string? Error)
{
    public static ServiceResult SuccessResult() => new(true, ErrorCode.None, null);
    public static ServiceResult Failure(ErrorCode code, string message) => new(false, code, message);
}
