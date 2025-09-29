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
    public static ServiceResult<T> SuccessResult(T value)
    {
        return new ServiceResult<T>(true, ErrorCode.None, null, value);
    }

    public static ServiceResult<T> Failure(ErrorCode code, string message)
    {
        return new ServiceResult<T>(false, code, message, default);
    }
}

public readonly record struct ServiceResult(bool Success, ErrorCode Code, string? Error)
{
    public static ServiceResult SuccessResult()
    {
        return new ServiceResult(true, ErrorCode.None, null);
    }

    public static ServiceResult Failure(ErrorCode code, string message)
    {
        return new ServiceResult(false, code, message);
    }
}