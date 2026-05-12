namespace PharmaFlow.Application.Common.Messaging;

public interface IIdempotentAppCommand : IAppCommand;

public interface IIdempotentAppCommand<TResult> : IAppCommand<TResult>;