using Mediator;

using PharmaFlow.Domain.Common;

namespace PharmaFlow.Application.Common.Messaging;

public interface IAppQuery<TResult> : IRequest<Result<TResult>>;