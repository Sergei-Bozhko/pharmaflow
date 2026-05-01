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