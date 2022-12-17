using Azure.Messaging.ServiceBus;
using DLQ.Common.Configuration.ChannelConfig;
using DLQ.Common.LoggerManager;
using DLQ.Message.Processor.Providers;
using IPA5.XO.ProtoBuf;
using Microsoft.Azure.ServiceBus.Management;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Timer = System.Threading.Timer;

namespace DLQ.MessageRetriever.Providers
{
    // https://learn.microsoft.com/en-us/windows/uwp/launch-resume/run-a-background-task-on-a-timer-
    public class AzureServiceBusTopicServer : IAzureServiceBusTopicServer, IDisposable
    {
        private int iterationCount = 0;

        private Timer refreshTimer;
        private int timeoutDelayMs;
        private ServiceBusConfiguration serviceBusConfiguration;

        private string filterRuleName;

        private IDeadLetterQueueProcessor deadLetterQueueProcessorImpl;

        public AzureServiceBusTopicServer(IDeadLetterQueueProcessor deadLetterQueueProcessorImpl)
            => this.deadLetterQueueProcessorImpl = deadLetterQueueProcessorImpl;

        public void Dispose()
        {
            StopBackgroundTask();
        }

        public async Task<string> ConnectAsync(ServiceBusConfiguration serviceBus, int timeoutDelaySec, string filter)
        {
            // set filtering rule
            if (!string.IsNullOrWhiteSpace(filter))
            {
                filterRuleName = $"FilterOn_{filter}";
            }

            await Task.Delay(1);

            StartBackgroundTask(serviceBus, timeoutDelaySec);

            return filterRuleName;
        }

        private void StartBackgroundTask(ServiceBusConfiguration serviceBusConfig, int timeoutDelaySec)
        {
            serviceBusConfiguration = serviceBusConfig;
            deadLetterQueueProcessorImpl.SetServiceBusConfiguration(serviceBusConfig);
            timeoutDelayMs = timeoutDelaySec * 1000;
            RegisterBackgroundTask(timeoutDelayMs);
        }

        private void StopBackgroundTask()
        {
            StopTimer();
        }

        private void RegisterBackgroundTask(int timeoutDelay)
        {
            refreshTimer = new Timer(
                callback: async (state) => await DoWorkAsyc(state),
                state: null,
                dueTime: timeoutDelay,
                period: timeoutDelay
            );
        }

        private void StopTimer()
        {
            if (refreshTimer != null)
            {
                refreshTimer.Change(Timeout.Infinite, Timeout.Infinite);
                refreshTimer.Dispose();
                refreshTimer = null;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Required by delegate.")]
        private async Task DoWorkAsyc(object state)
        {
            refreshTimer.Change(Timeout.Infinite, Timeout.Infinite);

            Console.WriteLine($"********* DLQ Lookup Iteration Count: {++iterationCount}");

            try
            {
                // Get Current Subscription List
                IList<SubscriptionDescription> subscriptionDescriptionList = await deadLetterQueueProcessorImpl.GetTopicSubscriptionList();

                if (subscriptionDescriptionList is { })
                {
                    foreach (SubscriptionDescription subscriptionDescription in subscriptionDescriptionList)
                    {
                        // Read messages from dead letter queue
                        IList<ServiceBusReceivedMessage> serviceBusReceivedMessageList = await deadLetterQueueProcessorImpl.ProcessDeadLetterQueueMessages(subscriptionDescription.SubscriptionName, false).ConfigureAwait(false);
                        System.Diagnostics.Debug.WriteLine($"Subscription {subscriptionDescription.SubscriptionName} has {serviceBusReceivedMessageList.Count} messages in DLQ.");

                        if (serviceBusReceivedMessageList.Count > 0)
                        {
                            // Remove messages from dead letter queue
                            IList<ServiceBusReceivedMessage> serviceBusRemovedMessageList = await deadLetterQueueProcessorImpl.ProcessDeadLetterQueueMessages(subscriptionDescription.SubscriptionName, true).ConfigureAwait(false);
      
                            if (serviceBusRemovedMessageList.Count > 0)
                            {
                                foreach (ServiceBusReceivedMessage message in serviceBusRemovedMessageList)
                                {
                                    ChannelData channelData = ChannelData.Parser.ParseFrom(message.Body.ToArray());

                                    if (channelData?.BrokerMessage is { } brokerMessage)
                                    {
                                        //_ = loggingServiceClient.LogInfoAsync($"Message {brokerMessage.StringData} successfully removed from Deadletter queue.");
                                        System.Diagnostics.Debug.WriteLine($"Message {brokerMessage.StringData} successfully removed from Deadletter queue.");
                                    }
                                }
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"No Messages removed from Deadletter queue.");
                            }

                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception in DLQ Provider - Message={ex.Message}");
                Logger.error($"Exception in DLQ Provider - Message={0}", ex.Message);
            }

            // Schedule a new timer
            RegisterBackgroundTask(timeoutDelayMs);
        }
    }
}
