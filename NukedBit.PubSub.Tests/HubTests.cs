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
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
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

        private class MockSubscriber : IHandleMessage<Message>
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
        public async Task ReceiveSingleMessage()
        {
            var hub = new Hub();
            var message = new Message("descr");
            var subscriber = new MockSubscriber { ConsumedAction = m => Assert.Equal("descr", m.Description) };
            hub.Subscribe(subscriber);
            await hub.Publish(message);
        }

        [Fact]
        public async Task ReceiveTwoMessage()
        {
            var hub = new Hub();
            var message = new Message("descr");
            var message2 = new Message("descr2");
            var expectedDescrs = new List<string> { "descr", "descr2" };
            var subscriber = new MockSubscriber
            {
                ConsumedAction = m =>
                {
                    expectedDescrs.Remove(m.Description);
                }
            };
            hub.Subscribe(subscriber);
            await hub.Publish(message);
            await hub.Publish(message2);
            Assert.Equal(0, expectedDescrs.Count);
        }

        [Fact]
        public async Task NotifyMultipleHandlers()
        {
            var hub = new Hub();
            var message = new Message("descr");
            var sub1Mock = new Mock<IHandleMessage<Message>>();
            var sub2Mock = new Mock<IHandleMessage<Message>>();
            sub1Mock.Setup(m => m.Consume(message)).Returns(Task.FromResult(0)).Verifiable();
            sub2Mock.Setup(m => m.Consume(message)).Returns(Task.FromResult(0)).Verifiable();

            hub.Subscribe(sub1Mock.Object);
            hub.Subscribe(sub2Mock.Object);
            await hub.Publish(message);

            sub1Mock.Verify(m=> m.Consume(message),Times.Once);
            sub2Mock.Verify(m=> m.Consume(message),Times.Once);
        }

        [Fact]
        public async Task UnSunscribe()
        {
            var hub = new Hub();
            var message = new Message("descr");
            var sub1Mock = new Mock<IHandleMessage<Message>>();
            sub1Mock.Setup(m => m.Consume(message)).Returns(Task.FromResult(0)).Verifiable();

            hub.Subscribe(sub1Mock.Object);
            hub.UnSubscribe(sub1Mock.Object);
            await hub.Publish(message);

            sub1Mock.Verify(m => m.Consume(message), Times.Never);
        }
    }
}
