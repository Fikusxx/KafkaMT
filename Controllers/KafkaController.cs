using Microsoft.AspNetCore.Mvc;
using KafkaMT.Messages;
using KafkaMT.Mediatr;
using MassTransit;
using MediatR;
using Newtonsoft.Json;
using KafkaMT.REPORT_TEST;
using KafkaMT.NEW_REPORT_TEST;
using System.Reflection;
using System.Collections.Frozen;

namespace KafkaMT.Controllers;

[ApiController]
[Route("kafka")]
public class KafkaController : ControllerBase
{
	private readonly ITopicProducer<KafkaMessage> producer;
	private readonly IMediator mediator;
	private readonly IBusControl busControl;

	public KafkaController(ITopicProducer<KafkaMessage> producer, IMediator mediator, IBusControl busControl)
	{
		this.producer = producer;
		this.mediator = mediator;
		this.busControl = busControl;
	}

	[HttpGet]
	public async Task<IActionResult> Test()
	{
		await producer.Produce(new KafkaMessage("Hello World"), Pipe.Execute<KafkaSendContext<KafkaMessage>>(ctx =>
		{
			// For testing
			ctx.MessageId = Guid.Parse("1441ab2e-7db3-48fe-8b38-f04710823c0e");
			//ctx.MessageId = Guid.NewGuid();

			ctx.FaultAddress = new Uri("http://kekw");
			ctx.DestinationAddress = new Uri("http://pogt");
			ctx.Headers.Set("Test Key", "Test Value");
			ctx.Headers.Set("Time", DateTimeOffset.UtcNow);

			// explicitly define the partition
			//ctx.Partition = 1;
		}));

		return Ok();
	}

	[HttpGet]
	[Route("idem-check")]
	public async Task<IActionResult> IdempotencyCheck()
	{
		var request = new Request() { IdempotencyKey = Guid.Parse("1441ab2e-7db3-48fe-8b38-f04710823c0e") };
		await mediator.Send(request);

		return Ok();
	}

	[HttpGet]
	[Route("complicated-1")]
	public async Task<IActionResult> One([FromServices] ITopicProducer<string, ComplicatedKafkaMessage> producer)
	{
		var options = new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.All };
		var message = new MessageOne("one");
		var e = new ComplicatedKafkaMessage()
		{
			EventId = Guid.NewGuid(),
			EventType = nameof(MessageOne),
			Event = JsonConvert.SerializeObject(message, options),
			Message = message
		};

		await producer.Produce("123", e);

		return Ok();
	}

	[HttpGet]
	[Route("complicated-2")]
	public async Task<IActionResult> Two([FromServices] ITopicProducer<string, ComplicatedKafkaMessage> producer)
	{
		var options = new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.All };

		var e = new ComplicatedKafkaMessage()
		{
			EventId = Guid.NewGuid(),
			EventType = nameof(MessageTwo),
			Event = JsonConvert.SerializeObject(new MessageTwo("two"), options)
		};

		await producer.Produce("123", e);

		return Ok();
	}

	[HttpGet]
	[Route("ECST-event")]
	public async Task<IActionResult> ECST()
	{
		var reportEventTypes = Assembly.GetAssembly(typeof(IReportEvent))!.GetTypes()
				.Where(x => typeof(IReportEvent).IsAssignableFrom(x) && !x.IsInterface)
				.Select(x => x.Name)
				.ToFrozenSet();

		var options = new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.All };
		var e = new ReportCreatedEvent() { Data = "123", ReportId = Guid.NewGuid() };

		if (e is IDomainEvent domainEvent)
		{
			var serialized = JsonConvert.SerializeObject(e, options);
			var typeName = domainEvent.GetType().Name;

			if (domainEvent is IIntergrationEvent intEvent)
			{
				var outbox = new MyIntegrationEvent(
					serialized,
					new EventMetadata(e.CorrelationId, typeName, "Admin")
					);

				if (reportEventTypes.Contains(outbox.Metadata.EventType))
				{
					await Console.Out.WriteLineAsync("Sent to some topic");
				}
			}

			if (e is IQueryEvent queryEvent)
			{
				var outbox = new MyQueryEvent(
					serialized,
					new EventMetadata(e.CorrelationId, typeName, "Admin")
					);

				if (reportEventTypes.Contains(outbox.Metadata.EventType))
				{
					await Console.Out.WriteLineAsync("Sent to some topic");
				}
			}
		}

		return Ok();
	}
}