using Foxrough.ServerSentEvents.Client.Infrastructure;
using Foxrough.ServerSentEvents.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Foxrough.ServerSentEvents.ConsoleAPp
{
    class Program
    {
        static void Main(string[] args)
        {
            var uri = GetArgValue(args, "uri");
            if (!Uri.IsWellFormedUriString(uri, UriKind.Absolute))
            {
                Console.WriteLine("missing valid arg : -uri http(s)://...");
                return;
            }

            var proxy = new ServerSentEventProxy(uri, new ConsoleLogger());
            proxy.DataReceived += ProxyOnDataReceived;
            Console.WriteLine("Press Q to quit...");
            proxy.Start();

            var exit = false;
            while (!exit)
            {
                var key = Console.ReadKey();
                exit = (key.KeyChar == 'Q' || key.KeyChar == 'q');
            }

            proxy.Stop();
        }

        private static void ProxyOnDataReceived(object sender, IReadOnlyCollection<EventMessage> messages)
        {
            foreach (var message in messages)
            {
                Console.WriteLine($"[Message] Id:{message.Id}, Event:{message.Event}, Retry:{message.Retry}, Data:{string.Join(", ", message.Data)}");
            }
        }

        private static string GetArgValue(string[] args, string argKey)
        {
            for (var i = 0; i < args.Length; i++)
            {
                if (!$"-{argKey}".Equals(args[i], StringComparison.InvariantCultureIgnoreCase)) continue;

                var indexOfValueCandidate = i + 1;
                if (args.Length >= indexOfValueCandidate + 1 && !args[indexOfValueCandidate]
                    .StartsWith("-", StringComparison.InvariantCultureIgnoreCase))
                    return args[indexOfValueCandidate];
                else
                    return string.Empty;
            }

            return string.Empty;
        }

    }

    internal sealed class ConsoleLogger : ILogger
    {
        /// <inheritdoc />
        public void Write(TraceLevel level, string className, string methodName, string message)
        {
            Console.WriteLine($"[{level}][{className}.{methodName}()]{message}");
        }
    }
}
