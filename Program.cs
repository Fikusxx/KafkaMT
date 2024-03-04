using System.Reflection;
using KafkaMT.Consumers;
using KafkaMT.Messages;
using KafkaMT.Services;
using KafkaMT.Mediatr;
using Confluent.Kafka;
using MassTransit;


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
		rider.AddConsumer<ComplicatedKafkaMessageConsumer>();

		// Producers
		// key assignment gets overriden by manual call Produce(key, value)
		rider.AddProducer<Guid, KafkaMessage>("demo1", m => Guid.NewGuid(), (ctx, producerCfg) =>
		{
			// Recommended for >= 1.0 Kafka 
			// Default for >= 3.0 Kafka
			producerCfg.EnableIdempotence = true;
			// will be set automatically along with EnableIdempotence = true
			//producerCfg.MessageSendMaxRetries = 10;
			//producerCfg.Acks = Acks.All (not really assignable with config here)

			// High throughput producer
			// waits for this amount of ms before sending messages
			//producerCfg.Linger = TimeSpan.FromMilliseconds(20); // default 5ms
			//producerCfg.CompressionType = CompressionType.Snappy;


			// buffers in memory if a broker cant accept any more messages
			//producerCfg.QueueBufferingMaxKbytes = 100 * 1024; // default 100mb
			//producerCfg.QueueBufferingMaxMessages = 100000; // default 100000
			
		});

		rider.AddProducer<string, KafkaMessageError>("error");

		rider.AddProducer<string, ComplicatedKafkaMessage>("demo");




		rider.UsingKafka((context, cfg) =>
		{
			//cfg.UseSendFilter(typeof(ContextSendFilter<>), context);

			cfg.ClientId = Assembly.GetExecutingAssembly().GetName().Name;
			cfg.SecurityProtocol = SecurityProtocol.SaslSsl;


			cfg.Host("pkc-7xoy1.eu-central-1.aws.confluent.cloud:9092", options =>
			{
				options.UseSasl(sasl =>
				{
					sasl.SecurityProtocol = SecurityProtocol.SaslSsl;
					sasl.Mechanism = SaslMechanism.Plain;
					sasl.Username = "U6ET6RBEIL6HNPER";
					sasl.Password = "Wr+WKq5GsivecybAStWeD0LaihRTt+OgkAd42jDGnHnU+ZZy1P3LWrqK7e/w48p5";
				});
			});

			var topic1Group = new ConsumerConfig()
			{
				GroupId = "group_1",
				AutoOffsetReset = AutoOffsetReset.Earliest,
				Acks = Acks.All

				// auto commits offsets after 5s since 1st poll, then it restarts
				//AutoCommitIntervalMs = 5000,
				//EnableAutoCommit = true,

				// doesnt work
				//PartitionAssignmentStrategy = PartitionAssignmentStrategy.CooperativeSticky,

				// allows consumers to leave a group and then join again w/o rebalancing partitions
				//GroupInstanceId = "123",

				// wwqweq
				//SessionTimeoutMs = 45000,
				//HeartbeatIntervalMs = 3000,


				// time a consumer would wait after a poll() that returned no messages
				// a consumer would essentially sit and wait for 300s for any messages to show up
				//MaxPollIntervalMs = 300000,
			};

			cfg.TopicEndpoint<Guid, KafkaMessage>("message", topic1Group, e =>
			{
				e.UseKillSwitch(k => k.SetActivationThreshold(10)
				.SetRestartTimeout(m: 1)
				.SetTripThreshold(0.2));
				
				// add to pipeline before consumers
				//e.UseConsumeFilter(typeof(IdempotencyFilter<>), context, x => x.Include(type => type.HasInterface<IHasIdempotency>()));

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

				// https://stackoverflow.com/questions/57258424/what-is-the-difference-between-concurrencylimit-and-prefetchcount
				// https://www.youtube.com/watch?v=M_yPAhWgvo4&ab_channel=ChrisPatterson
				// https://masstransit.io/documentation/configuration/transports/kafka
				// broker side
				// Number of Messages to prefetch from kafka topic into memory
				// MT automatically sets the prefetch count = number of CPU processors (cores) 
				// OR it adds a little buffer based on ConcurrentMessageLimit, unless specified explicitly
				// Example: ConcurrentMessageLimit = 10, then PrefetchCount will be ~12
				e.PrefetchCount = 10;

				// client side
				// recieves up to 10 message per partition, the number of concurrent messages, per partition
				// Preserving ordering with different keys.
				// When keys ARE SAME will use ConcurrentDeliveryLimit instead
				e.ConcurrentMessageLimit = 10;

				// Number of Confluent Consumer instances withing same endpoint
				// create up to two Kafka consumers, increases throughput with multiple partitions
				e.ConcurrentConsumerLimit = 2;

				// delivery only one message per key value within a partition at a time (default)
				// WILL BREAK ordering unless it's = 1 (default)
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


			// TEST
			var topic3Group = new ConsumerConfig()
			{
				GroupId = "group_3",
				AutoOffsetReset = AutoOffsetReset.Earliest
			};

			cfg.TopicEndpoint<ComplicatedKafkaMessage>("demo", topic3Group, e =>
			{
				//e.UseConsumeFilter(typeof(MyExceptionFilter<>), context);

				e.ConfigureConsumer<ComplicatedKafkaMessageConsumer>(context);
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