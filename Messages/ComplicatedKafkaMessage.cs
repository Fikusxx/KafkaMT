namespace KafkaMT.Messages;

public class ComplicatedKafkaMessage
{
	public Guid EventId { get; set; }
	public string EventType { get; set; }

	// other meta data

	public string Event { get; set; }
	public IMessage Message { get; set; }	
}

public record MessageOne(string value) : IMessage
{ }
public record MessageTwo(string value) : IMessage
{ }

public interface IMessage { }