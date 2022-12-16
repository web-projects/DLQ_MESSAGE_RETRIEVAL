using Azure.Messaging.ServiceBus;
using DLQ.Common.Configuration.ChannelConfig;
using DLQ.Common.LoggerManager;
using DLQ.Common.Utilities;
using DLQ.Message.Provider.Providers;
using Google.Protobuf;
using IPA5.XO.ProtoBuf;
using Microsoft.Azure.ServiceBus.Management;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace DLQ.Message.Processor.Providers
{
    public class DeadLetterQueueProcessorImpl : IDeadLetterQueueProcessor
    {
        private string filterRuleName;
        private string subscriptionKey;

        private List<SubscriptionDescription> subscriptionDescriptions = new List<SubscriptionDescription>();
        private List<SubscriptionDescription> subscriptionDescriptionsWorker = new List<SubscriptionDescription>();
        private IList<ServiceBusReceivedMessage> serviceBusReceivedMessageList = new List<ServiceBusReceivedMessage>();

        private SubscriptionFilter subscriptionFilter = new SubscriptionFilter(); 

        public List<SubscriptionDescription> GetTopicSubscriptions()
            => subscriptionDescriptions;

        public async Task<string> CreateFilterRule(ServiceBus serviceBusConfiguration, bool setDefaultRuleName, bool resetSubscriptionKey)
        {
            if (setDefaultRuleName)
            {
                subscriptionFilter.SetDefaultRuleName();
            }

            if (resetSubscriptionKey)
            {
                subscriptionFilter.ResetSubscriptionKey();
            }

            filterRuleName = await subscriptionFilter.SetFilter(serviceBusConfiguration).ConfigureAwait(false);
            subscriptionKey = subscriptionFilter.GetSubscriptionKey();
            return filterRuleName;
        }

        public async Task<bool> HasTopicSubscriptions(ServiceBus serviceBusConfiguration)
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
                Logger.info("Topic [{0}] has {1} active subscriptions.", serviceBusConfiguration.Topic, subscriptionDescriptions.Count());
            }

            return subscriptionDescriptions.Count() > 0;
        }

        /// <summary>
        /// https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-dead-letter-queues
        /// </summary>
        /// <param name="serviceBusConfiguration"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        public async Task WriteDLQMessages(ServiceBus serviceBusConfiguration, int NumberofMessagestoSend)
        {
            ServiceBusClient sbClient = new ServiceBusClient(serviceBusConfiguration.ConnectionString);

            // send messages to topic: listener-client-seek-dev
            ServiceBusSender sbSender = sbClient.CreateSender(serviceBusConfiguration.Topic);

            Console.WriteLine($"Sending messages to Topic '{serviceBusConfiguration.Topic}' for Subscription '{subscriptionKey}' ...");
            Logger.info("Sending messages to Topic '{0}' for Subscription '{1}' ...", serviceBusConfiguration.Topic, subscriptionKey);

            // send several messages to the queue
            for (int index = 0; index < NumberofMessagestoSend; index++)
            {
                ChannelData channelData = new ChannelData
                {
                    BrokerMessage = new BrokerMessage()
                    {
                        StringData = $"{{{serviceBusConfiguration.LastFilterNameUsed}}} - Test Message with index: {index}",
                        Header = new CommunicationHeader()
                        {
                            Flags = new CommFlags()
                            {
                                ServiceBusFilter = filterRuleName   // target subscription
                            }
                        }
                    }
                };

                try
                {
                    string messageId = Guid.NewGuid().ToString().Substring(0, 8);

                    ServiceBusMessage serviceBusMessage = new ServiceBusMessage(channelData.ToByteArray())
                    {
                        // used for BrokerBrokerOwnerId and Message filtering rule setting
                        Subject = serviceBusConfiguration.LastFilterNameUsed,
                        MessageId = messageId,
                        TimeToLive = TimeSpan.FromSeconds(serviceBusConfiguration.SubscriptionMessageTTLSec)
                    };

                    await sbSender.SendMessageAsync(serviceBusMessage).ConfigureAwait(false);

                    lock (Console.Out)
                    {
                        Console.WriteLine($"SENT {string.Format("{0:D3}", index)}: MessageId={serviceBusMessage.MessageId} - [{channelData.BrokerMessage.StringData}]");
                        Logger.info("SENT {0}: MessageId={1} - [{2}]", string.Format("{0:D3}", index), serviceBusMessage.MessageId, channelData.BrokerMessage.StringData);
                    }
                }
                catch (ServiceBusException e)
                {
                    Debug.WriteLine($"ServiceBusException in DLQ Provider - Message={e.Message}");
                    Logger.error("ServiceBusException in DLQ Provider - Message={0}", e.Message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception on send message={ex.Message}");
                }

                await Task.Delay(100);
            }

            // DisposeAsync and CloseAsync were documented to be the same, so using DisposeAsync
            await sbSender.DisposeAsync().ConfigureAwait(false);
            await sbClient.DisposeAsync().ConfigureAwait(false);
        }

        public async Task<IList<ServiceBusReceivedMessage>> ReadDeadLetterQueue(string subscriptionId, int messageCount)
        {
            await Task.Delay(1);
            return serviceBusReceivedMessageList;
        }

        public async Task ProcessMessagesInSubscription(ServiceBus serviceBusConfiguration, string subscriptionName, int numberMessagesToProcess)
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
                    Logger.info("Processing DLQ messages for Subscription: '{0}' ...", subscriptionName);

                    while (true)
                    {
                        ServiceBusReceivedMessage brokerActiveMessage = await sbReceiver.ReceiveMessageAsync(TimeSpan.FromMilliseconds(500)).ConfigureAwait(false);

                        if (brokerActiveMessage is { })
                        {
                            ChannelData channelData = ChannelData.Parser.ParseFrom(brokerActiveMessage.Body.ToArray());

                            if (channelData?.BrokerMessage is { })
                            {
                                lock (Console.Out)
                                {
                                    Console.WriteLine($"{string.Format("{0:D3}", ++counter)} Message: MessageId={brokerActiveMessage.MessageId} - [{channelData.BrokerMessage.StringData}]");
                                    Logger.info("{0} Message: MessageId={1} - [{2}]", string.Format("{0:D3}", counter), brokerActiveMessage.MessageId, channelData.BrokerMessage.StringData);
                                }

                                // Remove message from Active List
                                await sbReceiver.CompleteMessageAsync(brokerActiveMessage).ConfigureAwait(false);
                            }

                            if (counter >= numberMessagesToProcess)
                            {
                                break;
                            }
                        }
                        else
                        {
                            Console.WriteLine($"No active message. Total messages processed = {counter}");
                            Logger.info("No active message. Total messages processed = {0}", counter);
                            break;
                        }
                    }

                    // DisposeAsync and CloseAsync were documented to be the same, so using DisposeAsync
                    await sbReceiver.DisposeAsync().ConfigureAwait(false);
                    await sbClient.DisposeAsync().ConfigureAwait(false);
                }
            }
            catch (ServiceBusException e)
            {
                Debug.WriteLine($"ServiceBusException in DLQ Provider - Message={e.Message}");
                Logger.error("ServiceBusException in DLQ Provider - Message={0}", e.Message);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception in DLQ Provider - Message={ex.Message}");
            }
        }

        public async Task<bool> RemoveDLQMessages(ServiceBus serviceBusConfiguration)
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

                            ChannelData channelData = ChannelData.Parser.ParseFrom(brokerDLQMessage.Body.ToArray());

                            if (channelData?.BrokerMessage is { })
                            {
                                lock (Console.Out)
                                {
                                    Console.WriteLine($"DLQ: MessageId={brokerDLQMessage.MessageId} - [{channelData.BrokerMessage.StringData}]");
                                    Logger.info("DLQ: MessageId={0} - [{1}]", brokerDLQMessage.MessageId, channelData.BrokerMessage.StringData);
                                }

                                // Peform resources and task cleanup
                                // ToDO: send dead-letter-queue messages to reprocessing?

                                // Remove message from DLQ to processing...
                                await sbReceiver.CompleteMessageAsync(brokerDLQMessage).ConfigureAwait(false);
                            }

                            // Add message to List
                            serviceBusReceivedMessageList.Add(brokerDLQMessage);
                        }
                        else
                        {
                            break;
                        }
                    }

                    // DisposeAsync and CloseAsync were documented to be the same, so using DisposeAsync
                    await sbReceiver.DisposeAsync().ConfigureAwait(false);
                    await sbClient.DisposeAsync().ConfigureAwait(false);

                    // Delete last entry in the worker
                    subscriptionDescriptionsWorker.Remove(subcriptionDescription);
                }
            }
            catch (ServiceBusException e)
            {
                Debug.WriteLine($"ServiceBusException in DLQ Provider - Message={e.Message}");
                Logger.error("ServiceBusException in DLQ Provider - Message={0}", e.Message);
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
    }
}
