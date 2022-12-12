using DLQ.Common.Configuration.ChannelConfig;
using System.Threading.Tasks;

namespace DLQ.MessageRetriever.Providers
{
    public interface IAzureServiceBusTopicServer
    {
        Task<string> ConnectAsync(ServiceBus serviceBus, int timeoutDelaySec, string filter);
    }
}