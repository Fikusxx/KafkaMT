namespace KafkaMT.Messages;

public sealed record KafkaMessage(string Value) : IHasIdempotency
{ }

public sealed record KafkaMessageError()
{ }

public interface IHasIdempotency { }