using System.Threading.Tasks;

namespace NukedBit.PubSub
{
    public interface IHub : IPublisher, ISubscriber
    {
         
    }

    public interface IPublisher
    {
        Task Publish<T>(T message) where T : class;
    }

    public interface ISubscriber
    {
        void Subscribe<T>(IHandleMessage<T> handleMessage) where T : class;
        void UnSubscribe<T>(IHandleMessage<T> handleMessage) where T : class;
    }
}