using System.Reflection;
using KafkaMT.Consumers;
using KafkaMT.Messages;
using Confluent.Kafka;
using MassTransit;
using KafkaMT.Filters;
using MassTransit.Internals;
using KafkaMT.Services;
using KafkaMT.Mediatr;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IMyService, MyService>();

//builder.Services.AddSingleton<ISchemaRegistryClient>(provider =>
//{
//	var schemaRegistryConfig = new SchemaRegistryConfig
//	{
//		Url = "https://pkc-w7d6j.germanywestcentral.azure.confluent.cloud:443"
//	};

//	//return new CachedSchemaRegistryClient(schemaRegistryConfig);

//	return new CachedSchemaRegistryClient(new Dictionary<string, string>
//			{
//				{ "schema.registry.url", "pkc-w7d6j.germanywestcentral.azure.confluent.cloud:9092" },
//				{ "schema.registry.basic.auth.credentials.source", "SASL_INHERIT" },
//				{ "sasl.username", "qkjuokxx" },
//				{ "sasl.password", "JDpC2zG-1szh1NtFyNEoTQdss_rIgfcH" }
//			});
//});

builder.Services.AddMassTransit(x =>
{
	x.UsingInMemory((context, cfg) => cfg.ConfigureEndpoints(context));

	x.AddRider(rider =>
	{
		// Consumers
		rider.AddConsumer<KafkaMessageConsumer>();
		rider.AddConsumer<KafkaMessageErrorConsumer>();

		// Producers
		rider.AddProducer<KafkaMessage>("message", (ctx, producerCfg) =>
		{

		});
		rider.AddProducer<KafkaMessageError>("error");


		rider.UsingKafka((context, cfg) =>
		{
			//cfg.UseSendFilter(typeof(ContextSendFilter<>), context);

			cfg.ClientId = Assembly.GetExecutingAssembly().GetName().Name;
			cfg.SecurityProtocol = SecurityProtocol.SaslSsl;

			cfg.Host("pkc-w7d6j.germanywestcentral.azure.confluent.cloud:9092", options =>
			{
				options.UseSasl(sasl =>
				{
					sasl.Mechanism = SaslMechanism.Plain;
					sasl.Username = "UCWFG4WEOBMF6E3A";
					sasl.Password = "xk5seqwHeS5Ol6+3m649RQKE9Dxjuudl1nsEPn452ZWIyXK0OkZsjdV1MZCGqWtj";
				});
			});

			var topic1Group = new ConsumerConfig()
			{
				GroupId = "group_1",
				AutoOffsetReset = AutoOffsetReset.Earliest
			};

			cfg.TopicEndpoint<KafkaMessage>("message", topic1Group, e =>
			{
				e.UseKillSwitch(k => k.SetActivationThreshold(10)
				.SetRestartTimeout(m: 1)
				.SetTripThreshold(0.2));

				e.UseConsumeFilter(typeof(IdempotencyFilter<>), context, x => x.Include(type => type.HasInterface<IHasIdempotency>()));

				// doesnt work?
				e.CreateIfMissing(options =>
				{
					options.NumPartitions = 2;
					options.ReplicationFactor = 1;
				});

				// Transient error handling
				e.UseMessageRetry(retry => retry.Interval(2, TimeSpan.FromSeconds(1)));

				// doesnt work with Kafka
				//e.UseDelayedRedelivery(r => r.Intervals(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(15)));

				// recieves up to 10 message per partition, the number of concurrent messages, per partition
				e.ConcurrentMessageLimit = 10;

				// create up to two Kafka consumers, increases throughput with multiple partitions
				e.ConcurrentConsumerLimit = 2;

				// delivery only one message per key value within a partition at a time (default)
				e.ConcurrentDeliveryLimit = 1;

				e.ConfigureConsumer<KafkaMessageConsumer>(context);
			});

			var topic2Group = new ConsumerConfig()
			{
				GroupId = "group_2",
				AutoOffsetReset = AutoOffsetReset.Earliest
			};

			cfg.TopicEndpoint<KafkaMessageError>("error", topic2Group, e =>
			{
				e.ConfigureConsumer<KafkaMessageErrorConsumer>(context);
			});
		});
	});
});

#region MediatR

builder.Services.AddScoped<IdempotentcyCheckService>();
builder.Services.AddMediatR(x =>
{
	x.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
	x.AddOpenBehavior(typeof(IdempotencyBehavior<,>));
});

#endregion

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.Run();