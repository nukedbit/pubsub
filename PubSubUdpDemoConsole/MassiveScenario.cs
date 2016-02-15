using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Common.Logging;
using Common.Logging.Simple;
using Helios.Topology;
using NukedBit.PubSub;
using NukedBit.PubSub.Udp;

namespace PubSubDemoConsole
{
    public class MassiveScenario
    {
        public static int MessageCount = 1000000;
        private Consumer _consumer;

        public class MyEvent
        {
            public string EventType { get; set; }
            public DateTime EventTime { get; set; }
        }


        public class Consumer : IHandleMessage<MyEvent>
        {
            private readonly IHub _serverHub;

            public Consumer()
            {
                var logger = new ConsoleOutLogger("ConsumerLog", LogLevel.All, true, true, true, "dd/MM/yyyy", true);
                _serverHub = UdpHub.CreateServerHub(Node.Loopback(2556), Node.Loopback(2555), logger);
                _serverHub.Subscribe(this);
            }

            public Task Consume(MyEvent @event)
            {
                //   Console.WriteLine("Messsaggio: {0}", @event.EventType);
                if (@event.EventType == (MessageCount - 1).ToString())
                    Console.WriteLine((string)"Received {0} EventType", (object)MessageCount);
                return Task.FromResult(0);
            }
        }


        public async Task Run()
        {
            _consumer = new Consumer();
            await MassiveSend();
        }

        private async Task MassiveSend()
        {
            var logger = new ConsoleOutLogger("ClientLog", LogLevel.All, true, true, true, "dd/MM/yyyy", true);
            var udpHub = UdpHub.CreateClientHub(Node.Loopback(2555), Node.Loopback(2556), logger);
            Console.WriteLine("Publishing {0}", MessageCount);
            Stopwatch sw = Stopwatch.StartNew();
            var date = DateTime.Now;
            for (var i = 0; i < MessageCount; i++)
                await udpHub.Publish(new MyEvent { EventType = i.ToString(), EventTime = date });
            sw.Stop();
            Console.WriteLine("Time taken: {0}/sec", sw.Elapsed.TotalSeconds);
        }
    }
}