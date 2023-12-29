using MediatR;

namespace KafkaMT.Mediatr;

public class RequestHandler : IRequestHandler<Request>
{
	public Task Handle(Request request, CancellationToken cancellationToken)
	{
		var key = request.IdempotencyKey;

		// write stuff

		return Task.CompletedTask;
	}
}