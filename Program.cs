using Grpc.Core;
using Jaeger.Reporters;
using Jaeger.Samplers;
using Jaeger;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTracing;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System;


namespace grpc_test
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("ARGS: " + String.Join(", ", args));
            Console.WriteLine($"CNT: {args.Count()} | args[0] == '{args[0]}'");
            if (args.Count() >= 1 && args[0] == "ping") {
                Console.WriteLine("Doing the ping");
                DoThePing();
            } else {
                CreateHostBuilder(args).Build().Run();
            }
        }

        public static void DoThePing() {
            Environment.SetEnvironmentVariable("GRPC_TRACE", "api");
            Environment.SetEnvironmentVariable("GRPC_VERBOSITY", "debug");
            Grpc.Core.GrpcEnvironment.SetLogger(new Grpc.Core.Logging.ConsoleLogger());

            var targetA = "localhost:5001";
            var targetB = "localhost:5002";
            var nextTargetA = false;
            var chanceToDoIt = 0.90;
            var channel = new Channel(targetA, ChannelCredentials.Insecure);
            var client = new Bouncer.BouncerClient(channel);
            var reply = client.BounceIt(new BounceRequest {
                    TargetA = targetA,
                    TargetB = targetB,
                    DoTargetA = nextTargetA,
                    ChanceOfBounce = chanceToDoIt
                });
            Console.WriteLine($"Reply: {reply}");
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
