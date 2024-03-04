using KafkaMT.Messages;
using Newtonsoft.Json;
using MassTransit;

namespace KafkaMT.Consumers;

public class ComplicatedKafkaMessageConsumer : IConsumer<ComplicatedKafkaMessage>
{
	private static readonly JsonSerializerSettings options = new() { TypeNameHandling = TypeNameHandling.All };

	public Task Consume(ConsumeContext<ComplicatedKafkaMessage> context)
	{
		var e = JsonConvert.DeserializeObject(context.Message.Event, options);

		switch (context.Message.EventType)
		{
			case nameof(MessageOne):
				Console.WriteLine("Consumer #1: Its Message One Type");
				break;

			case nameof(MessageTwo):
				Console.WriteLine("Consumer #1: Its Message Two Type");
				break;
		}

		switch (e)
		{
			case MessageOne message:
				Console.WriteLine("Consumer #1: Message One");
				break;

			case MessageTwo message:
				Console.WriteLine("Consumer #1: Message Two");
				break;
		}

		if (context.Message.Message is MessageOne one)
		{
			Console.WriteLine("Its message one");
		}
		else if (context.Message.Message is MessageTwo two)
		{
			Console.WriteLine("Its message two");
		}

		return Task.CompletedTask;
	}
}
