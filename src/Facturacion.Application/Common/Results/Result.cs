namespace Facturacion.Application.Common.Results;
public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public IReadOnlyList<Error> Errors { get; }
    protected Result(bool success, IEnumerable<Error>? errors = null)
    {
        var values = errors?.ToArray() ?? Array.Empty<Error>();
        if (success && values.Length != 0 || !success && values.Length == 0)
            throw new ArgumentException("Estado y errores inconsistentes.");
        IsSuccess = success;
        Errors = Array.AsReadOnly(values);
    }
    public static Result Success() => new(true);
    public static Result Failure(params Error[] errors) => new(false, errors);
    public static Result<T> Success<T>(T value) => Result<T>.Success(value);
    public static Result<T> Failure<T>(params Error[] errors) => Result<T>.Failure(errors);
}
