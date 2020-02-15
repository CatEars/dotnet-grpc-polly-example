using Grpc.Core;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using System.Linq;
using System;


namespace grpc_test
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (args.Count() >= 1 && args[0] == "ping") {
                Console.WriteLine("Doing initial bounce");
                DoTheBounce();
            } else {
                CreateHostBuilder(args).Build().Run();
            }
        }

        public static void DoTheBounce() {
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
                    ChanceOfBounce = chanceToDoIt,
                    TabLevel = ""
                });
            Console.WriteLine($"Reply: {reply.Msg}");
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
