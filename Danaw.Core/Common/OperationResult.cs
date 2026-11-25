namespace Danaw.Core.Common;

public class OperationResult
{
    protected OperationResult(bool success, IEnumerable<string> errors)
    {
        Success = success;
        Errors = errors.ToList();
    }

    public bool Success { get; }
    public IReadOnlyList<string> Errors { get; }

    public static OperationResult Ok() => new(true, Array.Empty<string>());
    public static OperationResult Fail(string error) => new(false, new[] { error });
    public static OperationResult Fail(IEnumerable<string> errors) => new(false, errors);
}

public class OperationResult<T> : OperationResult
{
    private OperationResult(bool success, IEnumerable<string> errors, T? value)
        : base(success, errors)
    {
        Value = value;
    }

    public T? Value { get; }

    public static OperationResult<T> Ok(T value) => new(true, Array.Empty<string>(), value);
    public static new OperationResult<T> Fail(string error) => new(false, new[] { error }, default);
    public static new OperationResult<T> Fail(IEnumerable<string> errors) => new(false, errors, default);
}