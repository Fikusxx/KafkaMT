using KafkaMT.Mediatr;
using KafkaMT.Messages;
using MassTransit;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace KafkaMT.Controllers;

[ApiController]
[Route("kafka")]
public class KafkaController : ControllerBase
{
	private readonly ITopicProducer<KafkaMessage> producer;
	private readonly IMediator mediator;

	public KafkaController(ITopicProducer<KafkaMessage> producer, IMediator mediator)
	{
		this.producer = producer;
		this.mediator = mediator;
	}

	[HttpGet]
	public async Task<IActionResult> Test()
	{
		await producer.Produce(new KafkaMessage("Hello World"), Pipe.Execute<KafkaSendContext<KafkaMessage>>(ctx =>
		{
			ctx.MessageId = Guid.Parse("1441ab2e-7db3-48fe-8b38-f04710823c0e");
			//ctx.MessageId = Guid.NewGuid();
			//ctx.FaultAddress = new Uri("http://kekw");
			//ctx.FaultAddress = new Uri("loopback://localhost/kafka/topic_2");
			ctx.DestinationAddress = new Uri("http://pogt");
			ctx.Headers.Set("Test Key", "Test Value");
			ctx.Headers.Set("Time", DateTimeOffset.UtcNow);
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
}
