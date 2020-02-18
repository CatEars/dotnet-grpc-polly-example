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

        public static string EnvVarOrDefault(string varname, string defaultVal)
        {
            var env = Environment.GetEnvironmentVariable(varname);
            if (env == null || env == "") {
                return defaultVal;
            }
            return env;
        }

        public static void DoTheBounce() {
            Environment.SetEnvironmentVariable("GRPC_TRACE", "api");
            Environment.SetEnvironmentVariable("GRPC_VERBOSITY", "debug");
            Grpc.Core.GrpcEnvironment.SetLogger(new Grpc.Core.Logging.ConsoleLogger());

            var targetA = EnvVarOrDefault("TARGET_A", "localhost:5001");
            var targetB = EnvVarOrDefault("TARGET_B", "localhost:5002");
            Console.WriteLine($"A: {targetA} B: {targetB}");
            var nextTargetA = false;
            var chanceToDoIt = 0.70;
            var channel = new Channel(targetA, ChannelCredentials.Insecure);
            var client = new Bouncer.BouncerClient(channel);
            try {
                var reply = client.BounceIt(new BounceRequest {
                        TargetA = targetA,
                        TargetB = targetB,
                        DoTargetA = nextTargetA,
                        ChanceOfBounce = chanceToDoIt,
                        TabLevel = ""
                    });
                Console.WriteLine($"Reply: {reply.Msg}");
            } catch (RpcException e) {
                Console.WriteLine("Woops, looks like we got an exception. " +
                                  "Just make sure to run this a few times to get " +
                                  "some data for Jaeger.");
                Console.WriteLine("*** RpcException Start ***");
                Console.WriteLine(e);
                Console.WriteLine("*** RpcException End ***");
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
