using Grpc.Core;
using Jaeger.Reporters;
using Jaeger.Samplers;
using Jaeger.Senders;
using Jaeger;
using Microsoft.Extensions.Logging;
using OpenTracing.Propagation;
using OpenTracing;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;

namespace grpc_test
{
    public class BouncyService : Bouncer.BouncerBase
    {

        private ITracer _tracer;
        private readonly ILogger<BouncyService> _logger;
        public BouncyService(ILogger<BouncyService> logger)
        {
            _logger = logger;
            var loggerFactory = LoggerFactory.Create(builder => {
                    builder
                    .AddConsole();
                });
            var serviceName = Environment.GetEnvironmentVariable("JAEGER_SERVICE_NAME");
            var jaegerEndpoint = Environment.GetEnvironmentVariable("JAEGER_ENDPOINT");
            var sender = new HttpSender(jaegerEndpoint);
            var reporter = new RemoteReporter.Builder()
                .WithLoggerFactory(loggerFactory)
                .WithMaxQueueSize(1000)
                .WithFlushInterval(TimeSpan.FromSeconds(10))
                .WithSender(sender)
                .Build();
            _tracer = new Tracer.Builder(serviceName)
                .WithSampler(new ConstSampler(true))
                .WithLoggerFactory(loggerFactory)
                .WithReporter(reporter)
                .Build();

        }

        public Metadata CreateInjectPackageFromSpan(ISpan span)
        {
            var dict = new Dictionary<string, string>();
            _tracer.Inject(
                           span.Context, BuiltinFormats.HttpHeaders,
                           new TextMapInjectAdapter(dict));
            var meta = new Metadata();
            foreach (var entry in dict)
            {
                meta.Add(entry.Key, entry.Value);
            }
            return meta;
        }

        public IDictionary<string, string> MetadataToDictionary(Metadata meta)
        {
            var dict = new Dictionary<string, string>();
            foreach (var entry in meta)
            {
                dict.Add(entry.Key, entry.Value);
            }
            return dict;
        }

        public override Task<BounceReply>
            BounceIt(BounceRequest request, ServerCallContext context)
        {
            ISpanContext parentSpanCtx = null;
            try
            {
                var dict = MetadataToDictionary(context.RequestHeaders);
                parentSpanCtx = _tracer.Extract(BuiltinFormats.HttpHeaders,
                                                new TextMapExtractAdapter(dict));
                Console.WriteLine("Fixed a parent span from:");
                foreach(var entry in dict)
                {
                    Console.WriteLine($"{entry.Key} => {entry.Value}");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Could not read parentSpanCtx", e);
            }
            var builder = _tracer.BuildSpan("grpc://BounceIt");
            if (parentSpanCtx != null)
            {
                builder.AsChildOf(parentSpanCtx);
            }
            using (var scope = builder.StartActive(true))
            {
                var span = scope.Span;
                Console.WriteLine("Entered into bounceit");
                var chanceToDoIt = request.ChanceOfBounce;
                var randomizedChance = new Random().NextDouble();
                var returnMessage = "Hello!";
                if (chanceToDoIt >= randomizedChance)
                {
                    span.Log(DateTime.Now,
                             $"Doing another bounce: {chanceToDoIt} >= {randomizedChance}");
                    Console.WriteLine($"Do a bounce! {randomizedChance}");
                    var targetA = request.TargetA;
                    var targetB = request.TargetB;
                    var doTargetA = request.DoTargetA;
                    var nextTargetA = !doTargetA;
                    var target = doTargetA ? targetA : targetB;

                    var channel = new Channel(target, ChannelCredentials.Insecure);
                    var client = new Bouncer.BouncerClient(channel);
                    var dict = new Dictionary<string, string>();
                    var meta = CreateInjectPackageFromSpan(span);
                    var reply = client.BounceIt(new BounceRequest {
                            TargetA = targetA,
                            TargetB = targetB,
                            DoTargetA = nextTargetA,
                            ChanceOfBounce = chanceToDoIt
                        }, meta);
                    Console.WriteLine($"Bounce returned! {randomizedChance}");
                    returnMessage += " " + reply.Msg;
                }
                var sleepyTime = (int) (randomizedChance * 1000);
                Console.WriteLine($"Sleeping for {sleepyTime}");
                span.Log(DateTime.Now,
                         $"Sleeping for {sleepyTime}");
                Thread.Sleep(sleepyTime);
                Console.WriteLine($"Woke up");
                return Task.FromResult(new BounceReply
                {
                    Msg = returnMessage
                });
            }

        }
    }
}
