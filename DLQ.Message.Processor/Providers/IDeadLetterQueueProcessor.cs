using Azure.Messaging.ServiceBus;
using DLQ.Common.Configuration.ChannelConfig;
using Microsoft.Azure.ServiceBus.Management;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DLQ.Message.Processor.Providers
{
    public interface IDeadLetterQueueProcessor
    {
        void SetServiceBusConfiguration(ServiceBusConfiguration serviceBusConfig);
        Task<string> CreateFilterRule(ServiceBusConfiguration serviceBusConfiguration, bool setDefaultRuleName, bool resetSubscriptionKey);
        Task<IList<SubscriptionDescription>> GetTopicSubscriptionList();
        Task WriteDLQMessages(ServiceBusConfiguration serviceBusConfiguration, int NumberofMessagestoSend);
        //Task ProcessMessagesInSubscription(ServiceBusConfiguration serviceBusConfiguration, string subscriptionName, int numberMessagesToProcess);
        Task<IList<ServiceBusReceivedMessage>> ProcessDeadLetterQueueMessages(string subscriptionId, bool removeMessage);
        //Task<bool> RemoveDLQMessages(ServiceBus serviceBusConfiguration);
        //Task<IList<ServiceBusReceivedMessage>> RemoveMessagesFromDeadLetterQueueAsync(IList<ServiceBusReceivedMessage> serviceBusMessageList, string subscriptionKey);
    }
}