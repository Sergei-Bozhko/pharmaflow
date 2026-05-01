using PharmaFlow.Domain.Common;

namespace PharmaFlow.Tests.Unit.Common;

public class ResultTests
{
    [Fact]
    public void Success_carries_isSuccess_true_and_no_error()
    {
        var nonGeneric = Result.Success();
        Assert.True(nonGeneric.IsSuccess);
        Assert.False(nonGeneric.IsFailure);
        Assert.Equal(Error.None, nonGeneric.Error);

        var generic = Result<int>.Success(42);
        Assert.True(generic.IsSuccess);
        Assert.False(generic.IsFailure);
        Assert.Equal(Error.None, generic.Error);
        Assert.Equal(42, generic.Value);
    }

    [Fact]
    public void Failure_carries_isFailure_true_and_error()
    {
        var error = Error.NotFound("study.not_found", "Study was not found.");

        var nonGeneric = Result.Failure(error);
        Assert.False(nonGeneric.IsSuccess);
        Assert.True(nonGeneric.IsFailure);
        Assert.Equal(error, nonGeneric.Error);

        var generic = Result<int>.Failure(error);
        Assert.False(generic.IsSuccess);
        Assert.True(generic.IsFailure);
        Assert.Equal(error, generic.Error);
    }

    [Fact]
    public void Failere_with_None_error_throws()
    {
        Assert.Throws<InvalidOperationException>(() => Result.Failure(Error.None));
        Assert.Throws<InvalidOperationException>(() => Result<int>.Failure(Error.None));
    }

    [Fact]
    public void Asserting_Value_on_Faliure_throws()
    {
        var failure = Result<int>.Failure(Error.NotFound("x", "y"));
        Assert.Throws<InvalidOperationException>(() => _ = failure.Value);
    }

    [Fact]
    public void Implicit_T_to_ResultT_yields_Success()
    {
        Result<int> result = 42;
        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void Implicit_Error_to_ResultT_yields_Failure()
    {
        Result<int> result = Error.NotFound("study.not_found", "Not found.");
        Assert.True(result.IsFailure);
        Assert.Equal("study.not_found", result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.ErrorType);
    }

    [Fact]
    public void Implicit_Error_to_Result_yields_Failure()
    {
        Result result = Error.Conflict("study.invalid_transition", "Cannot activate from Closed.");
        Assert.True(result.IsFailure);
        Assert.Equal("study.invalid_transition", result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.ErrorType);
    }

    [Fact]
    public void Error_record_equality()
    {
        var a = Error.Validation("study.code.invalid", "Bad protocol number.");
        var b = Error.Validation("study.code.invalid", "Bad protocol number.");

        Assert.Equal(a, b);
        Assert.True(a == b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }
}