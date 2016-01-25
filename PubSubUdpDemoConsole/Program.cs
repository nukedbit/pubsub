using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Helios.Topology;
using NukedBit.PubSub;
using NukedBit.PubSub.Udp;

namespace PubSubDemoConsole
{

    public class MyMessage
    {
        public string Message { get; set; }
    }


    public class Consumer : IHandleMessage<MyMessage>
    {
        private readonly UdpHub _udpHub;

        public Consumer()
        {
            _udpHub = new UdpHub(Node.Loopback(2555), Node.Loopback(2556));
            _udpHub.Subscribe(this);
        }

        public Task Consume(MyMessage message)
        {
          //   Console.WriteLine("Messsaggio: {0}", message.Message);
            if (message.Message ==(Program.MessageCount-1).ToString())
                Console.WriteLine("Received {0} Messages", Program.MessageCount);
            return Task.FromResult(0);
        }
    }

    public class Program
    {
        private Consumer _consumer;

        public Program()
        {
            _consumer = new Consumer();
        }

        public static int MessageCount = 1000000;

        public static void Main(string[] args)
        {
            Console.Title = "Udp Console";
            new Program().Sender();
            Console.ReadLine();
        }

        private async Task Sender()
        {         
            var udpHub = new UdpHub(Node.Loopback(2556), Node.Loopback(2555));
            Console.WriteLine("Publishing {0}", MessageCount);
            Stopwatch sw = Stopwatch.StartNew();
            for (var i = 0; i < MessageCount; i++)
                 await udpHub.Publish(new MyMessage {Message = i.ToString()});
            sw.Stop();
            Console.WriteLine("Time taken: {0}ms", sw.Elapsed.TotalMilliseconds);
        }
    }
}
