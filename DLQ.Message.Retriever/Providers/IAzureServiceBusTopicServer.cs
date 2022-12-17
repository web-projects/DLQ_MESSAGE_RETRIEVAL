using DLQ.Common.Configuration.ChannelConfig;
using System.Threading.Tasks;

namespace DLQ.MessageRetriever.Providers
{
    public interface IAzureServiceBusTopicServer
    {
        Task<string> ConnectAsync(ServiceBusConfiguration serviceBus, int timeoutDelaySec, string filter);
    }
}