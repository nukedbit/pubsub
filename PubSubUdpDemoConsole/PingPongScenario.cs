using System;
using System.Threading.Tasks;
using Helios.Topology;
using NukedBit.PubSub;
using NukedBit.PubSub.Udp;

namespace PubSubDemoConsole
{
    public class PingPongScenario
    {
        private Client _client;
        private Server _server;

        public class Ping { }
        public class Pong { }


        public class Client : IHandleMessage<Pong>, IHandleMessage<ConnectionEstablished>
        {
            private readonly IHub _hub;
            public Client(IHub hub)
            {
                _hub = hub;
                _hub.Subscribe<Pong>(this);
                _hub.Subscribe<ConnectionEstablished>(this);
            }

            public async Task Consume(Pong message)
            {
                await Task.Delay(1);
                Console.WriteLine("Send Ping");
                await _hub.Publish(new Ping());
            }

            public async Task Consume(ConnectionEstablished message)
            {
                await _hub.Publish(new Ping());
            }
        }


        public class Server : IHandleMessage<Ping>
        {
            private readonly IHub _hub;
            public Server(IHub hub)
            {
                _hub = hub;
                _hub.Subscribe(this);
            }

            public async Task Consume(Ping message)
            {
                await Task.Delay(1);
                Console.WriteLine("Send Pong");
                await _hub.Publish(new Pong());

            }
        }


        public async Task Run()
        {
            var clientHub = new UdpHub(Node.Loopback(2556), Node.Loopback(2555));

            _client = new Client(clientHub);

            var serverHub = new UdpHub(Node.Loopback(2555), Node.Loopback(2556));
            _server = new Server(serverHub);

            await Task.Delay(TimeSpan.FromSeconds(2));
            await clientHub.Publish(new Ping());
        }
    }
}
