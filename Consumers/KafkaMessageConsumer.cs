using KafkaMT.Messages;
using MassTransit;

namespace KafkaMT.Consumers;

public sealed class KafkaMessageConsumer : IConsumer<KafkaMessage>
{
	public async Task Consume(ConsumeContext<KafkaMessage> context)
	{
		var key = context.GetKey<Guid>();
		var partition = context.Partition();
		var offset = context.Offset();
		var messageId = context.MessageId;
		var retry = context.GetRetryAttempt();
		var header = context.Headers.Get<string>("Test Key");
		var value = context.Headers.FirstOrDefault(x => x.Key == "Test Key").Value;
		var time2 = context.Headers.Get<DateTimeOffset>("Time");

		var timeValue = time2 is not null ? time2.Value.ToLocalTime() : DateTimeOffset.UtcNow.ToLocalTime();

        await Console.Out.WriteLineAsync($"{messageId} : key {key} partition {partition} offset {offset} at {timeValue}");

        // can be piped or used as is due to high events cohesion for granularity
        if (retry == 2)
		{
			var topicProducer = context.GetServiceOrCreateInstance<ITopicProducer<KafkaMessageError>>();
			await topicProducer.Produce(new KafkaMessageError());
			return;
		}

		//Console.WriteLine("Starting... #1");
		//throw new Exception();
	}
}

public sealed class KafkaMessageErrorConsumer : IConsumer<KafkaMessageError>
{
	public Task Consume(ConsumeContext<KafkaMessageError> context)
	{
		Console.WriteLine("Starting... #2");

		return Task.CompletedTask;
	}
}
