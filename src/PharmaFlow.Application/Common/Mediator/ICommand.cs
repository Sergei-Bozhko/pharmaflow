using Mediator;

using PharmaFlow.Domain.Common;

namespace PharmaFlow.Application.Common.Mediator;

public interface ICommand : IRequest<Result>;

public interface ICommand<TResult> : IRequest<Result<TResult>>;