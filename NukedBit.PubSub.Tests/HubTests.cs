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
using System.Threading.Tasks;
using Xunit;

namespace NukedBit.PubSub.Tests
{
    public class HubTests
    {
        public class Message
        {
            public readonly string Description;

            public Message(string description)
            {
                Description = description;
            }
        }

        private class MockSubscriber : IHandler<Message>
        {
            public Action<Message> ConsumedAction;

            public Task Consume(Message message)
            {
                ConsumedAction?.Invoke(message);
                return Task.FromResult(0);
            }
        }

        [Fact]
        public async Task Publish()
        {
            var hub = new Hub();
            await hub.Publish(new Message("descr"));
        }

        [Fact]
        public async Task ReceiveMessage()
        {
            var hub = new Hub();
            var message = new Message("descr");
            var subscriber = new MockSubscriber {ConsumedAction = m => Assert.Equal("descr", m.Description)};
            hub.Subscribe(subscriber);
            await hub.Publish(message);
        }
    }
}
