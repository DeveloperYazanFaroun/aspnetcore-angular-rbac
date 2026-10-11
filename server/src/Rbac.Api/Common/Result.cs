namespace Rbac.Api.Common;

/// <summary>
/// The kind of failure. The numeric value is the HTTP status code returned to the client.
/// </summary>
public enum ErrorType
{
    Validation = 400,
    Unauthorized = 401,
    Forbidden = 403,
    NotFound = 404,
    Conflict = 409,
}

/// <summary>
/// A failure with a message that is safe to show to the end user.
/// </summary>
public sealed record Error(ErrorType Type, string Message)
{
    public static Error Validation(string message) => new(ErrorType.Validation, message);
    public static Error Unauthorized(string message) => new(ErrorType.Unauthorized, message);
    public static Error Forbidden(string message) => new(ErrorType.Forbidden, message);
    public static Error NotFound(string message) => new(ErrorType.NotFound, message);
    public static Error Conflict(string message) => new(ErrorType.Conflict, message);
}

/// <summary>
/// The outcome of an operation that returns no value.
/// Business rules return a <see cref="Result"/> instead of throwing exceptions.
/// </summary>
public class Result
{
    protected Result(Error? error) => Error = error;

    public Error? Error { get; }

    public bool IsSuccess => Error is null;

    public static Result Success() => new(null);

    public static Result Failure(Error error) => new(error);

    public static implicit operator Result(Error error) => new(error);
}

/// <summary>
/// The outcome of an operation that returns a value on success.
/// </summary>
public sealed class Result<T> : Result
{
    private Result(T value) : base(null) => Value = value;

    private Result(Error error) : base(error)
    {
    }

    public T? Value { get; }

    public static Result<T> Success(T value) => new(value);

    public static new Result<T> Failure(Error error) => new(error);

    public static implicit operator Result<T>(T value) => new(value);

    public static implicit operator Result<T>(Error error) => new(error);
}
