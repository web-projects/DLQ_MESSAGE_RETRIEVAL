using Azure.Messaging.ServiceBus;
using DLQ.Common.Configuration.ChannelConfig;
using Microsoft.Azure.ServiceBus.Management;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DLQ.Message.Processor.Providers
{
    public interface IDeadLetterQueueProcessor
    {
        Task<string> CreateFilterRule(ServiceBus serviceBusConfiguration, bool resetSubscriptionKey);
        Task<bool> HasTopicSubscriptions(ServiceBus serviceBusConfiguration);
        List<SubscriptionDescription> GetTopicSubscriptions();
        Task<bool> ReadDLQMessages(ServiceBus serviceBusConfiguration);
        Task WriteDLQMessages(ServiceBus serviceBusConfiguration, int NumberofMessagestoSend);
        Task ProcessMessagesInSubscription(ServiceBus serviceBusConfiguration, string subscriptionName, int numberMessagesToProcess);
        Task<IList<ServiceBusReceivedMessage>> ReadDeadLetterQueue(string subscriptionId, int messageCount);
    }
}