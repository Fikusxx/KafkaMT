using MediatR;

namespace KafkaMT.Mediatr;

public interface IHasIdempotencyKey
{
	public Guid IdempotencyKey { get; }
}

public class Request : IRequest, IHasIdempotencyKey
{
	public required Guid IdempotencyKey { get; init; }
}