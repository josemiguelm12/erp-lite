namespace ErpLite.Application.Common;

public enum OperationStatus
{
    Success,
    NotFound,
    Forbidden,
    ValidationError
}

public sealed record OperationResult<T>(OperationStatus Status, T? Value, string? Error)
{
    public static OperationResult<T> Success(T value) => new(OperationStatus.Success, value, null);
    public static OperationResult<T> NotFound(string error) => new(OperationStatus.NotFound, default, error);
    public static OperationResult<T> Forbidden(string error) => new(OperationStatus.Forbidden, default, error);
    public static OperationResult<T> ValidationError(string error) => new(OperationStatus.ValidationError, default, error);
}

public sealed record OperationResult(OperationStatus Status, string? Error)
{
    public static OperationResult Success() => new(OperationStatus.Success, null);
    public static OperationResult NotFound(string error) => new(OperationStatus.NotFound, error);
    public static OperationResult Forbidden(string error) => new(OperationStatus.Forbidden, error);
    public static OperationResult ValidationError(string error) => new(OperationStatus.ValidationError, error);
}
