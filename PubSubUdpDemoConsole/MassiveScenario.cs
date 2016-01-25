using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Helios.Topology;
using NukedBit.PubSub;
using NukedBit.PubSub.Udp;

namespace PubSubDemoConsole
{
    public class MassiveScenario
    {
        public static int MessageCount = 1000000;
        private Consumer _consumer;

        public class MyMessage
        {
            public string Message { get; set; }
        }


        public class Consumer : IHandleMessage<MyMessage>
        {
            private readonly UdpSubscriber _subscriber;

            public Consumer()
            {
                _subscriber = new UdpSubscriber(Node.Loopback(2556));
                _subscriber.Subscribe(this);
            }

            public Task Consume(MyMessage message)
            {
                //   Console.WriteLine("Messsaggio: {0}", message.Message);
                if (message.Message == (MessageCount - 1).ToString())
                    Console.WriteLine((string) "Received {0} Message", (object) MessageCount);
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
            var udpHub = new UdpPublisher(Node.Loopback(2556), Node.Loopback(2555));
            Console.WriteLine("Publishing {0}", MessageCount);
            Stopwatch sw = Stopwatch.StartNew();
            for (var i = 0; i < MessageCount; i++)
                await udpHub.Publish(new MyMessage { Message = i.ToString() });
            sw.Stop();
            Console.WriteLine("Time taken: {0}ms", sw.Elapsed.TotalMilliseconds);
        }
    }
}