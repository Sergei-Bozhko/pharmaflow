namespace PharmaFlow.Application.Common.Mediator;

public interface IIdempotentCommand : ICommand;

public interface IIdempotentCommand<TResult> : ICommand<TResult>;