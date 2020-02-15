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

        private static Metadata CreateInjectMetadataFromSpan(ITracer tracer, ISpan span)
        {
            var dict = new Dictionary<string, string>();
            var injectAdapter = new TextMapInjectAdapter(dict);
            tracer.Inject(span.Context, BuiltinFormats.HttpHeaders, injectAdapter);
            var meta = new Metadata();
            foreach (var entry in dict)
            {
                meta.Add(entry.Key, entry.Value);
            }
            return meta;
        }

        private static IDictionary<string, string> MetadataToDictionary(Metadata meta)
        {
            var dict = new Dictionary<string, string>();
            foreach (var entry in meta)
            {
                dict.Add(entry.Key, entry.Value);
            }
            return dict;
        }

        private static void PrintRequestHeaders(ServerCallContext context, string prefix)
        {
            Console.WriteLine(prefix + "- gRPC Request Headers -");
            foreach (var entry in context.RequestHeaders)
            {
                Console.WriteLine(prefix + $"   [{entry.Key}] => '{entry.Value}'");
            }
            Console.WriteLine(prefix + "- end of gRPC Request Headers -");
        }

        private static ISpanContext ExtractSpanContextOrDefault(
          ITracer tracer,
          ServerCallContext context,
          ISpanContext defaultVal = null)
        {
            ISpanContext spanContext = defaultVal;
            try {
                var dict = MetadataToDictionary(context.RequestHeaders);
                var adapter = new TextMapExtractAdapter(dict);
                spanContext = tracer.Extract(BuiltinFormats.HttpHeaders, adapter);
            } catch {}
            return spanContext;
        }

        public override Task<BounceReply>
            BounceIt(BounceRequest request, ServerCallContext context)
        {
            var T = request.TabLevel;
            Console.WriteLine(T + "BounceIt() got a request.");
            PrintRequestHeaders(context, T);
            var builder = _tracer.BuildSpan("grpc://BounceIt");

            ISpanContext parentSpanCtx = ExtractSpanContextOrDefault(_tracer, context);
            if (parentSpanCtx != null)
            {
                builder.AsChildOf(parentSpanCtx);
            }

            using (var scope = builder.StartActive(true))
            {
                var span = scope.Span;
                var chanceToDoIt = request.ChanceOfBounce;
                var randomizedChance = new Random().NextDouble();
                var returnMessage = "Hello!";

                if (chanceToDoIt >= randomizedChance)
                {
                    span.Log(DateTime.Now,
                             $"Doing another bounce: {chanceToDoIt} >= {randomizedChance}");
                    var targetA = request.TargetA;
                    var targetB = request.TargetB;
                    var doTargetA = request.DoTargetA;
                    var nextTargetA = !doTargetA;
                    var target = doTargetA ? targetA : targetB;

                    var channel = new Channel(target, ChannelCredentials.Insecure);
                    var client = new Bouncer.BouncerClient(channel);

                    var bounceRequest = new BounceRequest {
                        TargetA = targetA,
                        TargetB = targetB,
                        DoTargetA = nextTargetA,
                        ChanceOfBounce = chanceToDoIt,
                        TabLevel = request.TabLevel + "  "
                    };
                    var metadata = CreateInjectMetadataFromSpan(_tracer, span);
                    Console.WriteLine(T + "Doing another bounce!");
                    var reply = client.BounceIt(bounceRequest, metadata);
                    returnMessage += " " + reply.Msg;
                }
                var sleepyTimeMs = (int) (randomizedChance * 1000);

                Console.Write(T + $"Sleeping for {sleepyTimeMs}ms...");
                span.Log(DateTime.Now,
                         $"Sleeping for {sleepyTimeMs}ms");
                Thread.Sleep(sleepyTimeMs);
                Console.WriteLine($"Woke up");

                return Task.FromResult(new BounceReply
                {
                    Msg = returnMessage
                });
            }

        }
    }
}
