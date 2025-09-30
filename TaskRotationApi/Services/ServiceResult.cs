namespace TaskRotationApi.Services;

/// <summary>
///     Represents the type of error returned by domain operations.
/// </summary>
public enum ErrorCode
{
    None = 0,
    Duplicate,
    NotFound,
    Invalid,
    LimitReached
}

/// <summary>
///     Encapsulates the outcome of an operation returning a value.
/// </summary>
/// <typeparam name="T">The type of the value produced by the operation.</typeparam>
public readonly record struct ServiceResult<T>(bool Success, ErrorCode Code, string? Error, T? Value)
{
    /// <summary>
    ///     Creates a successful <see cref="ServiceResult{T}"/>.
    /// </summary>
    /// <param name="value">The resulting value of the operation.</param>
    /// <returns>A successful service result containing the provided value.</returns>
    public static ServiceResult<T> SuccessResult(T value)
    {
        return new ServiceResult<T>(true, ErrorCode.None, null, value);
    }

    /// <summary>
    ///     Creates a failed <see cref="ServiceResult{T}"/>.
    /// </summary>
    /// <param name="code">The error code describing the failure.</param>
    /// <param name="message">A human readable message explaining the failure.</param>
    /// <returns>A failed service result.</returns>
    public static ServiceResult<T> Failure(ErrorCode code, string message)
    {
        return new ServiceResult<T>(false, code, message, default);
    }
}

/// <summary>
///     Encapsulates the outcome of an operation that does not produce a value.
/// </summary>
public readonly record struct ServiceResult(bool Success, ErrorCode Code, string? Error)
{
    /// <summary>
    ///     Creates a successful <see cref="ServiceResult"/>.
    /// </summary>
    /// <returns>A successful service result.</returns>
    public static ServiceResult SuccessResult()
    {
        return new ServiceResult(true, ErrorCode.None, null);
    }

    /// <summary>
    ///     Creates a failed <see cref="ServiceResult"/>.
    /// </summary>
    /// <param name="code">The error code describing the failure.</param>
    /// <param name="message">A human readable message explaining the failure.</param>
    /// <returns>A failed service result.</returns>
    public static ServiceResult Failure(ErrorCode code, string message)
    {
        return new ServiceResult(false, code, message);
    }
}