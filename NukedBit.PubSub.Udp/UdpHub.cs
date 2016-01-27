/*****************************************************************************
    Copyright 2015 Sebastian Faltoni

    Licensed under the Apache License, Version 2.0 (the "License");
    you may not use this file except in compliance with the License.
    You may obtain a copy of the License at

        http://www.apache.org/licenses/LICENSE-2.0

    Unless required by applicable law or agreed to in writing, software
    distributed under the License is distributed on an "AS IS" BASIS,
    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
    See the License for the specific language governing permissions and
    limitations under the License.
******************************************************************************/

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Helios.Exceptions;
using Helios.Net;
using Helios.Net.Bootstrap;
using Helios.Reactor;
using Helios.Reactor.Bootstrap;
using Helios.Topology;
using Newtonsoft.Json;

namespace NukedBit.PubSub.Udp
{
    public class UdpHub : IHub, IDisposable
    {
        private readonly IConnection _connection;
        private readonly INode _remoteEnpoint;

        private readonly ConcurrentDictionary<Type, List<object>> _subscrivers = new ConcurrentDictionary<Type, List<object>>();
        private readonly ConcurrentQueue<byte[]> _receivedEnvelops = new ConcurrentQueue<byte[]>();
        private bool _disposed;
        private Task _listener;
        private CancellationTokenSource _cancellationTokenSource;

        public static UdpHub CreateClientHub(INode remoteNode, INode localEndPoint)
        {
            return new UdpHub(new ClientBootstrap(), remoteNode, localEndPoint);
        }
        public static UdpHub CreateServerHub(INode remoteNode, INode localEndPoint)
        {
            return new UdpHub(new ServerBootstrap(), remoteNode, localEndPoint);
        }

        private UdpHub(ClientBootstrap clientBootstrap, INode remoteNode, INode localEndPoint)
        {
            _connection = clientBootstrap
                .SetTransport(TransportType.Udp)
                .RemoteAddress(remoteNode)
                .WorkerThreads(2)
                .OnConnect(OnConnectionEstablished)
                .OnReceive(OnReceivedData)
                .OnDisconnect(OnConnectionTerminated)
                .Build().NewConnection(localEndPoint, remoteNode);
            _connection.Open();
            _remoteEnpoint = remoteNode;
            StartListener();
        }

        private UdpHub(ServerBootstrap clientBootstrap, INode remoteNode, INode localEndPoint)
        {

            _connection = clientBootstrap
                .SetTransport(TransportType.Udp)
                .Build().NewReactor(localEndPoint).ConnectionAdapter;
            _connection.OnError += _connection_OnError;
            _connection.OnConnection += OnConnectionEstablished;
            _connection.OnDisconnection += OnConnectionTerminated;
            _connection.Receive += OnReceivedData;
            _connection.Open();
            _remoteEnpoint = remoteNode;
            StartListener();
        }

        private void _connection_OnError(Exception ex, IConnection connection)
        {

        }

        private void StartListener()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _listener = new Task(ListenIncomingMessages, _cancellationTokenSource.Token, TaskCreationOptions.LongRunning);
            _listener.Start();
        }

        private async void ListenIncomingMessages()
        {
            await DoDispatch();
        }

        private async Task DoDispatch()
        {
            try
            {
                while (!_cancellationTokenSource.IsCancellationRequested)
                {
                    if (_receivedEnvelops.IsEmpty)
                    {
                        await Task.Delay(1);
                        continue;
                    }
                    byte[] bts;
                    if (_receivedEnvelops.TryDequeue(out bts))
                    {
                        var json = Encoding.UTF8.GetString(bts);
                        var envelop = JsonConvert.DeserializeObject<UdpMessageEnvelop>(json);
                        var content = DeserializeContent(envelop);
                        await NotifySubscribers(content);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                //TODO: proper log
            }
        }


        private async Task NotifySubscribers(object content)
        {
            await Task.Run(async () =>
            {
                try
                {
                    List<object> handlers;
                    var messageType = content.GetType();
                    if (!_subscrivers.TryGetValue(messageType, out handlers))
                        return;
                    foreach (var handler in handlers)
                    {
                        var h = handler
                            .GetType()
                            .GetRuntimeMethods()
                            .Single(p => p.Name == "Consume" && p.GetParameters().Single().ParameterType == messageType);
                        await (Task) h.Invoke(handler, new[] {content});
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    // TODO: log
                }
            });
        }

        private async void OnConnectionTerminated(HeliosConnectionException reason, IConnection closedchannel)
        {
            await SendConnectionTerminated(reason, closedchannel);
        }

        private async Task SendConnectionTerminated(HeliosConnectionException reason, IConnection closedchannel)
        {
            try
            {
                var msg = new ConnectionTerminated()
                {
                    Host = closedchannel?.RemoteHost?.ToUri(),
                    Exception = reason
                };
                Console.WriteLine("ConnectionTerminated");
                await Publish<ConnectionTerminated>(msg);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                //TODO: proper log
            }
        }

        private async void OnConnectionEstablished(INode remoteaddress, IConnection responsechannel)
        {
            Console.WriteLine("OnConnectionEstablished {0}", remoteaddress.ToString());
            await SendConnectionEstablished(remoteaddress);
        }

        private async Task SendConnectionEstablished(INode remoteaddress)
        {
            try
            {
                var msg = new ConnectionEstablished()
                {
                    Host = remoteaddress.ToUri(),
                };

                await Publish<ConnectionEstablished>(msg);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                //TODO: proper log
            }
        }


        private void OnReceivedData(NetworkData incomingdata, IConnection responsechannel)
        {
            if (incomingdata.Buffer != null)
                _receivedEnvelops.Enqueue(incomingdata.Buffer);
        }

        private object DeserializeContent(UdpMessageEnvelop envelop)
        {
            var contentType = Type.GetType(envelop.ContentType);
            return JsonConvert.DeserializeObject(envelop.Content, contentType);
        }

        public async Task Publish<T>(T message) where T : class
        {
            await Task.Run(() =>
            {
                var envelop = new UdpMessageEnvelop
                {
                    ContentType = typeof(T).AssemblyQualifiedName,
                    Content = JsonConvert.SerializeObject(message, Formatting.None)
                };
                var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(envelop, Formatting.None));
                if (!_connection.IsOpen())
                    _connection.Open();
                _connection.Send(bytes, 0, bytes.Length, _remoteEnpoint);
            });
        }

        public void Subscribe<T>(IHandleMessage<T> handleMessage) where T : class
        {
            _subscrivers.AddOrUpdate(typeof(T), new List<object> { handleMessage }, (t, l) =>
            {
                l.Add(handleMessage);
                return l;
            });
        }

        public void UnSubscribe<T>(IHandleMessage<T> handleMessage) where T : class
        {
            List<object> handlers;
            if (_subscrivers.TryGetValue(typeof(T), out handlers))
            {
                var old = handlers;
                handlers.Remove(handleMessage);
                _subscrivers.TryUpdate(typeof(T), handlers, old);
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _cancellationTokenSource?.Cancel();
                _connection.Close();
                _connection.Dispose();
                _disposed = true;
            }
        }
    }
}