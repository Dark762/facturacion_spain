namespace Facturacion.Application.Common.Results;
public sealed class Result<T> : Result
{
    private readonly T? _value;
    public T Value => IsSuccess ? _value! : throw new InvalidOperationException("El resultado no contiene un valor.");
    private Result(T? value, bool success, IEnumerable<Error>? errors = null) : base(success, errors)
        => _value = value;
    public static Result<T> Success(T value) => new(value, true);
    public static new Result<T> Failure(params Error[] errors) => new(default, false, errors);
}
