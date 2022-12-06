using Azure.Messaging.ServiceBus;
using DLQ.MessageRetrieval.Configuration;
using DLQ.MessageRetrieval.Messages;
using DLQ.MessageRetrieval.Providers;
using DLQ.MessageRetrieval.Utilities;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace DeadletterQueue.Providers
{
    public static class DLQMessageProcessor
    {
        private const int MaxMessagestoSend = 10;
        private const int MaxWaitforMessageInDLQinMS = 5000;

        private static string filterRuleName;
        private static string subscriptionKey;

        private static BrokerMessage GetBrokerMessageFromBinaryData(BinaryData messageBinaryData)
            => (BrokerMessage)ArrayUtils.FromByteArray(messageBinaryData.ToArray());

        public static async Task<string> CreateFilterRule(ServiceBus serviceBusConfiguration)
        {
            filterRuleName = await SubscriptionFilter.SetFilter(serviceBusConfiguration).ConfigureAwait(false);
            subscriptionKey = SubscriptionFilter.GetSubscriptionKey();
            return filterRuleName;
        }

        public static async Task ReadDLQMessages(ServiceBus serviceBusConfiguration)
        {
            ServiceBusClient sbClient = new ServiceBusClient(serviceBusConfiguration.ConnectionString);

            string deadletterQueuePath = serviceBusConfiguration.DeadLetterQueuePath;
            string subscriptionName = subscriptionKey + deadletterQueuePath;

            Debug.WriteLine($"DLQ topic name _: {serviceBusConfiguration.Topic}");
            Debug.WriteLine($"DLQ subscription: {subscriptionName}");

            // DLQ Subscription
            ServiceBusReceiver sbReceiver = sbClient.CreateReceiver(serviceBusConfiguration.Topic, subscriptionName);

            int counter = 0;

            Console.WriteLine("Processing DLQ messages...");

            while (true)
            {
                ServiceBusReceivedMessage brokerDLQMessage = await sbReceiver.ReceiveMessageAsync(TimeSpan.FromMilliseconds(500)).ConfigureAwait(false);

                if (brokerDLQMessage is { })
                {
                    Console.WriteLine($"{string.Format("{0:D3}", counter++)} - [{brokerDLQMessage.DeadLetterErrorDescription}] - Reason: {brokerDLQMessage.DeadLetterReason}");

                    BrokerMessage brokerMessage = GetBrokerMessageFromBinaryData(brokerDLQMessage.Body);

                    if (brokerMessage is { })
                    {
                        lock (Console.Out)
                        {
                            Console.WriteLine($"DLQ: MessageId={brokerDLQMessage.MessageId} - [{brokerMessage.StringData}]");
                        }

                        // Peform resources and task cleanup
                        // ToDO

                        // Remove message from DLQ to processing...
                        await sbReceiver.CompleteMessageAsync(brokerDLQMessage).ConfigureAwait(false);
                    }
                }
                else
                {
                    break;
                }
            }

            await sbReceiver.DisposeAsync();
        }

        /// <summary>
        /// https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-dead-letter-queues
        /// </summary>
        /// <param name="serviceBusConfiguration"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        public static async Task WriteDLQMessages(ServiceBus serviceBusConfiguration)
        {
            ServiceBusClient sbClient = new ServiceBusClient(serviceBusConfiguration.ConnectionString);

            // send messages to topic: listener-client-seek-dev
            ServiceBusSender sbSender = sbClient.CreateSender(serviceBusConfiguration.Topic);

            Console.WriteLine($"Sending messages to topic '{serviceBusConfiguration.Topic}'...");

            // send several messages to the queue
            for (int index = 0; index < MaxMessagestoSend; index++)
            {
                BrokerMessage brokerMessage = new BrokerMessage()
                {
                    StringData = $"{{{serviceBusConfiguration.LastFilterNameUsed}}} - Test Message with index: {index}",
                    Header = new CommunicationHeader()
                    {
                        Flags = new CommFlags()
                        {
                            ServiceBusFilter = filterRuleName   // target subscription
                        }
                    }
                };

                try
                {
                    string messageId = Guid.NewGuid().ToString().Substring(0, 8);

                    ServiceBusMessage serviceBusMessage = new ServiceBusMessage(ArrayUtils.ToByteArray(brokerMessage))
                    {
                        // used for BrokerBrokerOwnerId and Message filtering rule setting
                        Subject = serviceBusConfiguration.LastFilterNameUsed,
                        MessageId = messageId,
                        TimeToLive = TimeSpan.FromSeconds(serviceBusConfiguration.SubscriptionMessageTTLSec)
                    };

                    await sbSender.SendMessageAsync(serviceBusMessage);

                    lock (Console.Out)
                    {
                        Console.WriteLine($"SENT {string.Format("{0:D3}", index)}: MessageId={serviceBusMessage.MessageId} - [{brokerMessage.StringData}]");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception on send message={ex.Message}");
                }

                await Task.Delay(100);
            }

            await sbSender.DisposeAsync();
        }
    }
}
