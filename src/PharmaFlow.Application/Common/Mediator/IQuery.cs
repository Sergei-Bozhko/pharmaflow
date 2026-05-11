using Mediator;

namespace PharmaFlow.Application.Common.Mediator;

public interface IQuery<TResult> : IRequest<TResult>;