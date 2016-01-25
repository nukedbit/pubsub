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
using System.Threading.Tasks;
using Helios.Exceptions;
using Helios.Net;
using Helios.Net.Bootstrap;
using Helios.Net.Connections;
using Helios.Topology;
using Newtonsoft.Json;

namespace NukedBit.PubSub.Udp
{

    public class UdpMessageEnvelop
    {
        public IDictionary<string,string> Headers { get; } = new Dictionary<string, string>();

        public string Content { get; set; }
    }

    public class UdpHub : IHub
    {
        private readonly IConnection _connection;
        private readonly INode _remoteEnpoint;

        private readonly ConcurrentDictionary<Type, List<object>> _subscrivers = new ConcurrentDictionary<Type, List<object>>();

        public UdpHub(INode remoteEnpoint, INode localEndPoint)
        {
            _connection = new ClientBootstrap()
                .SetTransport(TransportType.Udp)
                .RemoteAddress(localEndPoint)
                .OnConnect(ConnectionEstablishedCallback)
                .OnReceive(ReceivedDataCallback)
                .OnDisconnect(ConnectionTerminatedCallback)
                .Build().NewConnection(localEndPoint, remoteEnpoint);

            _connection.Open();
            _remoteEnpoint = remoteEnpoint;

        }

        private void ConnectionTerminatedCallback(HeliosConnectionException reason, IConnection closedchannel)
        {
             
        }

        private void ConnectionEstablishedCallback(INode remoteaddress, IConnection responsechannel)
        {
            
        }

        private async void ReceivedDataCallback(NetworkData incomingdata, IConnection responsechannel)
        {
            var json = Encoding.UTF8.GetString(incomingdata.Buffer);
            var envelop = JsonConvert.DeserializeObject<UdpMessageEnvelop>(json);
            var content = DeserializeContent(envelop);
            await NotifySubscribers(content);
        } 

        private async Task NotifySubscribers(object content)
        {
            await Task.Run(async () =>
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
            });
        }


        private object DeserializeContent(UdpMessageEnvelop envelop)
        {
            var contentType = Type.GetType(envelop.Headers["X-Type"]);
            return JsonConvert.DeserializeObject(envelop.Content, contentType);
        }

        public async Task Publish<T>(T message) where T : class
        {
            if (!_connection.IsOpen())
                await _connection.OpenAsync();
            var envelop = new UdpMessageEnvelop();
            envelop.Headers.Add("X-Type",typeof(T).AssemblyQualifiedName);
            envelop.Content = JsonConvert.SerializeObject(message,Formatting.None);
            var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(envelop, Formatting.None));
            _connection.Send(bytes,0, bytes.Length, _remoteEnpoint);
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
    }
}
