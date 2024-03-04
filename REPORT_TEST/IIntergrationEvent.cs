using System.Runtime.Serialization;
using System.Collections.Frozen;
using System.Reflection;


namespace KafkaMT.REPORT_TEST;

public interface IDomainEvent { } // required for AR to work

public interface IReportEvent { } // if there are many models for different topics, this is basically a marker interface to where u publish

public interface IHasCorrelationId
{
	[IgnoreDataMember] // so it doesnt appear in json
	public Guid CorrelationId { get; }
}
public interface IIntergrationEvent : IHasCorrelationId // published to generic topic, used by interceptor to assign CorrelationId for Outbox
{ }

public interface IQueryEvent : IHasCorrelationId // published to query topic, used by interceptor to assign CorrelationId for Outbox
{ }

public interface IAnalyticsEvent : IHasCorrelationId // or whatever events u want
{ }

public sealed class ReportCreatedEvent : IDomainEvent, IIntergrationEvent, IQueryEvent, IAnalyticsEvent, IReportEvent
{
	public Guid ReportId { get; set; }
	public string Data { get; set; }
	public Guid CorrelationId => ReportId;
}

public sealed class IntergrationOutbox // for generic resource topic
{
	public Guid EventId { get; set; }
	public Guid CorrelationId { get; set; }
	public string EventType { get; set; }
	public string EventData { get; set; }
	// other meta data..
}

public sealed class QueryOutbox { } // for query API topic
public sealed class AnalyticsOutbox { } // for analytics or w/e topic

public sealed class IntegrationEvent // being listened by ACL/orchestrator
{
	public Guid EventId { get; set; }
	public Guid CorrelationId { get; set; }
	public string EventType { get; set; }
	public string EventData { get; set; }
	// other meta data..
}

public sealed class MyPublishService
{
	private readonly FrozenSet<string> reportEventTypes;
	private readonly object outboxRepo;

	public MyPublishService()
	{
		reportEventTypes = Assembly.GetAssembly(typeof(IReportEvent))!.GetTypes()
			.Where(x => typeof(IReportEvent).IsAssignableFrom(x) && !x.IsInterface)
			.Select(x => x.Name)
			.ToFrozenSet();
	}

	public void Publish()
	{
		// get outbox messages

		// start loop
		// check its EventType against frozenSets
		// pack into a corresponsing IntegrationEvent and publish to a specific topic

		// delete outbox messages when loop is done
	}
}
