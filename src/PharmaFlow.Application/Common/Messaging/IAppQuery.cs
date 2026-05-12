using Mediator;

namespace PharmaFlow.Application.Common.Messaging;

public interface IAppQuery<TResult> : IRequest<TResult>;