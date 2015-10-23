using System.Threading.Tasks;

namespace NukedBit.PubSub
{
    public interface IHub
    {
        Task Publish<T>(T message) where T : class;
        void Subscribe<T>(IHandleMessage<T> handleMessage) where T : class;
        void UnSubscribe<T>(IHandleMessage<T> handleMessage) where T : class;
    }
}