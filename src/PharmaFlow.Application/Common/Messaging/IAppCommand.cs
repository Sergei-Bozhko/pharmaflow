using Mediator;

using PharmaFlow.Domain.Common;

namespace PharmaFlow.Application.Common.Messaging;

public interface IAppCommand : IRequest<Result>;

public interface IAppCommand<TResult> : IRequest<Result<TResult>>;