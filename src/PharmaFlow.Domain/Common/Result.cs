namespace PharmaFlow.Domain.Common;

public class Result
{
    public bool IsSuccess {get;}
    public bool IsFalure => !IsSuccess;
    public Error Error {get;}

    protected internal Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
        {
            throw new InvalidOperationException("A successful Result must carry Error.None.");
        }
        if(!isSuccess && error == Error.None)
        {
            throw new InvalidOperationException("A failed Result must carry a non-None error.");
        }
        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, Error.None);
    public static Result Failure(Error error) => new(false, error);

    public static implicit operator Result(Error error) => Failure(error);
}

public sealed class Result<T> : Result
{
    private readonly T _value;

    public T Value =>
        IsSuccess
            ? _value
            : throw new InvalidOperationException("Cannot access Value on a failure Result.");
    
    private Result(T value) : base(true, Error.None)
    {
        _value = value;
    }
    
    private Result(Error error): base(false, error)
    {
        _value = default!;
    }

    public static Result<T> Success(T value) => new(value);

    public static new Result<T> Failure(Error error) => new(error);

    public static implicit operator Result<T>(T value) => Success(value);
    public static implicit operator Result<T>(Error error) => Failure(error);
}