using System;
using System.Threading.Tasks;
using Helios.Reactor.Bootstrap;
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
            private readonly IHub _clientHub; 

            public Client(IHub clientHub)
            {
                _clientHub = clientHub; ;
                _clientHub.Subscribe<Pong>(this);
                _clientHub.Subscribe<ConnectionEstablished>(this);
            }

            public async Task Consume(Pong message)
            {
                await Task.Delay(1);
                Console.WriteLine("Send Ping");
                await _clientHub.Publish(new Ping());
            }

            public async Task Consume(ConnectionEstablished message)
            {
                await _clientHub.Publish(new Ping());
            }
        }


        public class Server : IHandleMessage<Ping>, IHandleMessage<ConnectionEstablished>
        {
            private readonly IHub _serverHub; 

            public Server(IHub serverHub)
            {
                _serverHub = serverHub;
                _serverHub.Subscribe<Ping>(this);
                _serverHub.Subscribe<ConnectionEstablished>(this);
            }

            public async Task Consume(Ping message)
            {
                await Task.Delay(1);
                Console.WriteLine("Send Pong");
                await _serverHub.Publish(new Pong());
            }

            public async Task Consume(ConnectionEstablished message)
            {
                Console.WriteLine("Send Pong");
                await _serverHub.Publish(new Pong());
            }
        }


        public async Task Run()
        {
            var clientHub = UdpHub.CreateClientHub(Node.Loopback(2557), Node.Loopback(2556));
            _client = new Client(clientHub);

            _server = new Server(UdpHub.CreateServerHub(Node.Loopback(2556), Node.Loopback(2557)));
            Console.WriteLine("Waiting..");
            await Task.Delay(TimeSpan.FromSeconds(2));
            Console.WriteLine("Publish");
            await clientHub.Publish(new Ping());
        }
    }
}
