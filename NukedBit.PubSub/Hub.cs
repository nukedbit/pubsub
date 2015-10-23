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
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace NukedBit.PubSub
{
    public sealed class Hub
    {
        private readonly ConcurrentDictionary<Type, object> _subscrivers = new ConcurrentDictionary<Type, object>();
        public async Task Publish<T>(T message) where T : class
        {
            object handler;
            var messageType = typeof(T);
            if (!_subscrivers.TryGetValue(messageType, out handler))
                return;

            var h = handler
                .GetType()
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.InvokeMethod)
                .Single(p => p.Name =="Consume" && p.GetParameters().Single().ParameterType == messageType);
            await (Task)h.Invoke(handler, BindingFlags.InvokeMethod | BindingFlags.Public, null, new object[] {message}, null);
        }

        public void Subscribe<T>(IHandler<T> handler) where T : class
        {
            _subscrivers.TryAdd(typeof(T), handler);
        }
    }
}