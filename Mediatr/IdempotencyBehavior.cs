
using MediatR;

namespace KafkaMT.Mediatr;

public class IdempotencyBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> 
	where TRequest : class, IHasIdempotencyKey
{
	private readonly IdempotentcyCheckService service;

	public IdempotencyBehavior(IdempotentcyCheckService service)
	{
		this.service = service;
	}

	public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
	{
		var isProcessed = await service.IsProcessed(request, request.IdempotencyKey);

		if (isProcessed)
			return default;

		// write to db { request, key }
		// saveChanges will be called in the requestHandler itself

		return await next.Invoke();
	}
}
