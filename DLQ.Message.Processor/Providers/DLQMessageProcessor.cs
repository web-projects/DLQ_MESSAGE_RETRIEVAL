using Azure.Messaging.ServiceBus;
using DLQ.Common.Configuration.ChannelConfig;
using DLQ.Common.Utilities;
using DLQ.MessageProcessor.Messages;
using Microsoft.Azure.ServiceBus.Management;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace DLQ.MessageProvider.Providers
{
    public static class DLQMessageProcessor
    {
        private static string filterRuleName;
        private static string subscriptionKey;

        private static List<SubscriptionDescription> subscriptionDescriptions = new List<SubscriptionDescription>();
        private static List<SubscriptionDescription> subscriptionDescriptionsWorker = new List<SubscriptionDescription>();

        private static BrokerMessage GetBrokerMessageFromBinaryData(BinaryData messageBinaryData)
            => (BrokerMessage)ArrayUtils.FromByteArray(messageBinaryData.ToArray());

        public static List<SubscriptionDescription> GetTopicSubscriptions()
            => subscriptionDescriptions;

        public static async Task<string> CreateFilterRule(ServiceBus serviceBusConfiguration, bool resetSubscriptionKey)
        {
            if (resetSubscriptionKey)
            {
                SubscriptionFilter.ResetSubscriptionKey();
            }
            filterRuleName = await SubscriptionFilter.SetFilter(serviceBusConfiguration).ConfigureAwait(false);
            subscriptionKey = SubscriptionFilter.GetSubscriptionKey();
            return filterRuleName;
        }

        public static async Task<bool> HasTopicSubscriptions(ServiceBus serviceBusConfiguration)
        {
            if (subscriptionDescriptionsWorker.Count() == 0)
            {
                ManagementClient managementClient = new ManagementClient(serviceBusConfiguration.ManagementConnectionString);

                for (int skip = 0; skip < 1000; skip += 100)
                {
                    IList<SubscriptionDescription> subscriptions = await managementClient.GetSubscriptionsAsync(serviceBusConfiguration.Topic, 100, skip);

                    if (!subscriptions.Any())
                    {
                        break;
                    }

                    subscriptionDescriptions.AddRange(subscriptions);
                }

                subscriptionDescriptionsWorker = subscriptionDescriptions;

                Console.WriteLine($"Topic [{serviceBusConfiguration.Topic}] has {subscriptionDescriptions.Count()} active subscriptions.\r\n");
            }

            return subscriptionDescriptions.Count() > 0;
        }

        public static async Task<bool> ReadDLQMessages(ServiceBus serviceBusConfiguration)
        {
            if (subscriptionDescriptionsWorker.Count() == 0)
            {
                return false;
            }

            int counter = 0;
            SubscriptionDescription subcriptionDescription = subscriptionDescriptionsWorker.First();

            try
            {
                if (!string.IsNullOrWhiteSpace(subcriptionDescription.SubscriptionName))
                {
                    string deadletterQueuePath = serviceBusConfiguration.DeadLetterQueuePath;
                    string subscriptionName = subcriptionDescription.SubscriptionName + deadletterQueuePath;

                    Debug.WriteLine($"DLQ topic name _: {serviceBusConfiguration.Topic}");
                    Debug.WriteLine($"DLQ subscription: {subscriptionName}");

                    // DLQ Subscription
                    ServiceBusClient sbClient = new ServiceBusClient(serviceBusConfiguration.ConnectionString);
                    ServiceBusReceiver sbReceiver = sbClient.CreateReceiver(serviceBusConfiguration.Topic, subscriptionName);

                    Console.WriteLine($"Processing DLQ messages for Subscription: '{subcriptionDescription.SubscriptionName}' ...");

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
                                // ToDO: send dead-letter-queue messages to reprocessing?

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

                    // Delete last entry in the worker
                    subscriptionDescriptionsWorker.Remove(subcriptionDescription);
                }
            }
            catch (ServiceBusException e)
            {
                Debug.WriteLine($"ServiceBusException in DLQ Provider - Message={e.Message}");
                if (e.Reason == ServiceBusFailureReason.MessagingEntityNotFound)
                {
                    subscriptionDescriptionsWorker.Remove(subcriptionDescription);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception in DLQ Provider - Message={ex.Message}");
            }

            return counter > 0;
        }

        /// <summary>
        /// https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-dead-letter-queues
        /// </summary>
        /// <param name="serviceBusConfiguration"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        public static async Task WriteDLQMessages(ServiceBus serviceBusConfiguration, int NumberofMessagestoSend)
        {
            ServiceBusClient sbClient = new ServiceBusClient(serviceBusConfiguration.ConnectionString);

            // send messages to topic: listener-client-seek-dev
            ServiceBusSender sbSender = sbClient.CreateSender(serviceBusConfiguration.Topic);

            Console.WriteLine($"Sending messages to Topic '{serviceBusConfiguration.Topic}' for Subscription '{subscriptionKey}' ...");

            // send several messages to the queue
            for (int index = 0; index < NumberofMessagestoSend; index++)
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

        public static async Task ProcessMessagesInSubscription(ServiceBus serviceBusConfiguration, string subscriptionName, int numberMessagesToProcess)
        {
            int counter = 0;

            try
            {
                if (!string.IsNullOrWhiteSpace(subscriptionName) && numberMessagesToProcess > 0)
                {
                    Debug.WriteLine($"DLQ topic name _: {serviceBusConfiguration.Topic}");
                    Debug.WriteLine($"DLQ subscription: {subscriptionName}");

                    // DLQ Subscription
                    ServiceBusClient sbClient = new ServiceBusClient(serviceBusConfiguration.ConnectionString);
                    ServiceBusReceiver sbReceiver = sbClient.CreateReceiver(serviceBusConfiguration.Topic, subscriptionName,
                        new ServiceBusReceiverOptions() 
                        { 
                            PrefetchCount = numberMessagesToProcess,
                            ReceiveMode = ServiceBusReceiveMode.PeekLock
                        });

                    Console.WriteLine($"Processing DLQ messages for Subscription: '{subscriptionName}' ...");

                    while (true)
                    {
                        ServiceBusReceivedMessage brokerActiveMessage = await sbReceiver.ReceiveMessageAsync(TimeSpan.FromMilliseconds(500)).ConfigureAwait(false);

                        if (brokerActiveMessage is { })
                        {
                            BrokerMessage brokerMessage = GetBrokerMessageFromBinaryData(brokerActiveMessage.Body);

                            if (brokerMessage is { })
                            {
                                lock (Console.Out)
                                {
                                    Console.WriteLine($"{string.Format("{0:D3}", ++counter)} Message: MessageId={brokerActiveMessage.MessageId} - [{brokerMessage.StringData}]");
                                }

                                // Remove message from Active List
                                await sbReceiver.CompleteMessageAsync(brokerActiveMessage).ConfigureAwait(false);
                            }

                            if (counter == numberMessagesToProcess)
                            {
                                break;
                            }
                        }
                        else
                        {
                            break;
                        }
                    }

                    await sbReceiver.DisposeAsync();
                }
            }
            catch (ServiceBusException e)
            {
                Debug.WriteLine($"ServiceBusException in DLQ Provider - Message={e.Message}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception in DLQ Provider - Message={ex.Message}");
            }
        }
    }
}
