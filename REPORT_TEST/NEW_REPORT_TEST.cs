using KafkaMT.REPORT_TEST;
using Newtonsoft.Json;

namespace KafkaMT.NEW_REPORT_TEST;


public class MessageWrapper<T> where T : class, IIntergrationEvent
{
	private static readonly JsonSerializerSettings options = new() { TypeNameHandling = TypeNameHandling.All };

	public MessageWrapper(T data, string? executedBy) 
	{
		Data = JsonConvert.SerializeObject(data, options);
		Metadata = new EventMetadata(data.CorrelationId, data.GetType().Name, executedBy);
	}

	public string Data { get; set; }
	public EventMetadata Metadata { get; set; }
}

public class EventMetadata
{
	public Guid EventId { get; set; }
	public Guid CorrelationId { get; set; }
	public string EventType { get; set; }
	public DateTimeOffset OccuredOn { get; set; }
	public string ExecutedBy { get; set; }

	public EventMetadata(Guid correlationId, string eventType, string? executedBy = null)
	{
		EventId = Guid.NewGuid();
		CorrelationId = correlationId;
		EventType = eventType;
		OccuredOn = DateTimeOffset.UtcNow;
		ExecutedBy = executedBy ?? "system";
	}
}

public class MyQueryEvent
{
	public string Data { get; set; }
	public EventMetadata Metadata { get; set; }

	public MyQueryEvent(string data, EventMetadata metadata)
	{
		Data = data;
		Metadata = metadata;
	}
}

public class MyIntegrationOutboxMessage
{
	public string Data { get; set; }
	public EventMetadata Metadata { get; set; }

	public MyIntegrationOutboxMessage(string data, EventMetadata metadata)
	{
		Data = data;
		Metadata = metadata;
	}
}

public sealed class MyIntegrationEvent
{
	public string Data { get; set; }
	public EventMetadata Metadata { get; set; }


	public MyIntegrationEvent(string data, EventMetadata metadata)
	{
		Data = data;
		Metadata = metadata;
	}
}