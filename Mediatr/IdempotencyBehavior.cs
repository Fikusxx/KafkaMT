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
		var isProcessed = await service.IsProcessed(request);

		// if that request has already been processed - break the pipeline
		if (isProcessed)
			return default;

		// write to db { id, requestName, requestId }
		// saveChanges will be called in the requestHandler itself

		return await next.Invoke();
	}
}
