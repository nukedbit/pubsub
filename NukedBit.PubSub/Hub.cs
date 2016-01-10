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
using System.Reflection;
using System.Threading.Tasks;

namespace NukedBit.PubSub
{
    public sealed class Hub : IHub
    {
        private readonly ConcurrentDictionary<Type, List<object>> _subscrivers = new ConcurrentDictionary<Type, List<object>>();
        public async Task Publish<T>(T message) where T : class
        {
            await Task.Run(async () =>
            {
                List<object> handlers;
                var messageType = typeof(T);
                if (!_subscrivers.TryGetValue(messageType, out handlers))
                    return;
                foreach (var handler in handlers)
                {
                    var h = handler
                        .GetType()
                        .GetRuntimeMethods()
                        .Single(p => p.Name == "Consume" && p.GetParameters().Single().ParameterType == messageType);
                    await (Task)h.Invoke(handler, new object[] { message });
                }
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
    }
}