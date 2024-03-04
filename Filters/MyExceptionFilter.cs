using KafkaMT.Messages;
using MassTransit;

namespace KafkaMT.Filters;

public class MyExceptionFilter<T> : IFilter<ConsumeContext<T>> where T : class
{
	private readonly ITopicProducer<KafkaMessage> producer;

	public MyExceptionFilter(ITopicProducer<KafkaMessage> producer)
	{
		this.producer = producer;
	}

	public void Probe(ProbeContext context)
	{

	}

	public async Task Send(ConsumeContext<T> context, IPipe<ConsumeContext<T>> next)
	{
		try
		{
			await next.Send(context);
		}
		catch (Exception)
		{
			Console.WriteLine("Error occured"); ;
			throw;
		}
	}
}