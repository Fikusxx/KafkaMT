using KafkaMT.Services;
using MassTransit;

namespace KafkaMT.Filters;

public sealed class IdempotencyFilter<T> : IFilter<ConsumeContext<T>> where T : class
{
	private readonly IMyService myService;

	public IdempotencyFilter(IMyService myService)
	{
		this.myService = myService;
	}

	public void Probe(ProbeContext context)
	{ }

	public async Task Send(ConsumeContext<T> context, IPipe<ConsumeContext<T>> next)
	{
		var test = await myService.CheckIdempotency();

		var time = context.Headers.Get<DateTimeOffset>("Time");

		await Console.Out.WriteLineAsync($"{context.MessageId} Idempotency check start at {time!.Value.ToLocalTime()}");

        if (context.MessageId == Guid.Parse("1441ab2e-7db3-48fe-8b38-f04710823c0e"))
		{
            await Console.Out.WriteLineAsync($"{context.MessageId} Idempotency check fail at {time!.Value.ToLocalTime()}");
            return;
		}

		await next.Send(context);
	}
}

//public sealed class IdempotencyConsumeContextProxy<T> : ConsumeContextProxy where T : class, IHasIdempotency
//{
//	public IdempotencyConsumeContextProxy(ConsumeContext context) : base(context)
//	{ }

//	public override bool TryGetMessage<T>(out ConsumeContext<T> consumeContext)
//	{
//		if (base.TryGetMessage(out consumeContext))
//			return true;

//		if (base.TryGetMessage<T>(out ConsumeContext<T>? messageContext))
//		{
//			if (messageContext.MessageId == Guid.Parse("1441ab2e-7db3-48fe-8b38-f04710823c0e"))
//				return false;
//		}

//		return false;
//	}
//}

//public sealed class IdempotencyConsumeContextSpecification<T> : IPipeSpecification<ConsumeContext>
//	where T : class, IHasIdempotency
//{
//	public void Apply(IPipeBuilder<ConsumeContext> builder)
//	{
//		builder.AddFilter(new IdempotencyFilter<T>());
//	}

//	public IEnumerable<ValidationResult> Validate()
//	{
//		yield break;
//	}
//}
