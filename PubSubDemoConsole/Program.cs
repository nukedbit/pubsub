using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading.Tasks;
using Helios.Net.Connections;
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
        private Socket _socket;
        private UdpConnection _connection;
        private UdpHub _udpHub;

        public Consumer()
        {
            _udpHub = new UdpHub(Node.Loopback(2555), Node.Loopback(2556));
            _udpHub.Subscribe(this);
        }

        public Task Consume(MyMessage message)
        {
            //Console.WriteLine("Messsaggio: {0}", message.Message);
            if (message.Message ==(Program.MessageCount-1).ToString())
                Console.WriteLine("Finito {0}", Program.MessageCount);
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
            Stopwatch sw = Stopwatch.StartNew();
            for (var i = 0; i < MessageCount; i++)
                 await udpHub.Publish(new MyMessage {Message = i.ToString()});
            sw.Stop();
            Console.WriteLine("Time taken: {0}ms", sw.Elapsed.TotalMilliseconds);
        }
    }
}
