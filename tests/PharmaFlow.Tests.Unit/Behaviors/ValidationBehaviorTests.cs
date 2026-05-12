using FluentValidation;

using Mediator;

using PharmaFlow.Application.Common.Behaviors;
using PharmaFlow.Domain.Common;

namespace PharmaFlow.Tests.Unit.Behaviors;

public class ValidationBehaviorTests
{
    [Fact]
    public async Task Empty_validators_passes_through_to_handlerAsync()
    {
        var behavior = new ValidationBehavior<TestRequest, Result>([]);
        var called = false;
        MessageHandlerDelegate<TestRequest, Result> next = (_, _) =>
        {
            called = true;
            return ValueTask.FromResult(Result.Success());
        };

        var result = await behavior.Handle(new TestRequest("ok", "fine"), next, CancellationToken.None);

        Assert.True(called);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Single_validator_failure_short_circuits_with_validation_errorAsync()
    {
        var validator = new InlineValidator<TestRequest>();
        validator.RuleFor(x => x.Name).NotEmpty();

        var behavior = new ValidationBehavior<TestRequest, Result>([validator]);
        var called = false;
        MessageHandlerDelegate<TestRequest, Result> next = (_, _) =>
        {
            called = true;
            return ValueTask.FromResult(Result.Success());
        };

        var result = await behavior.Handle(new TestRequest("", "fine"), next, CancellationToken.None);

        Assert.False(called);
        Assert.True(result.IsFailure);
        Assert.Equal("validation", result.Error.Code);
        Assert.Equal(ErrorType.Validation, result.Error.ErrorType);
        Assert.Contains("Name", result.Error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Multiple_failures_aggregate_in_messageAsync()
    {
        var validator = new InlineValidator<TestRequest>();
        validator.RuleFor(x => x.Name).NotEmpty();
        validator.RuleFor(x => x.Description).MaximumLength(5);

        var behavior = new ValidationBehavior<TestRequest, Result>([validator]);
        MessageHandlerDelegate<TestRequest, Result> next =
            (_, _) => ValueTask.FromResult(Result.Success());

        var result = await behavior.Handle(
            new TestRequest("", "way too long description here"),
            next,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("Name", result.Error.Message, StringComparison.Ordinal);
        Assert.Contains("Description", result.Error.Message, StringComparison.Ordinal);
    }

    public sealed record TestRequest(string Name, string Description) : IRequest<Result>;
}