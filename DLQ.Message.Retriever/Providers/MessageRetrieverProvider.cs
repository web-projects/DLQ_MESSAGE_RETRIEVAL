using DLQ.Common.Configuration.ChannelConfig;
using DLQ.Common.LoggerManager;
using DLQ.Message.Processor.Providers;
using DLQ.Message.Retriever;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Timer = System.Threading.Timer;

namespace DLQ.MessageRetriever.Providers
{
    // https://learn.microsoft.com/en-us/windows/uwp/launch-resume/run-a-background-task-on-a-timer-
    public class MessageRetrieverProvider
    {
        private int iterationCount = 0;

        private Timer refreshTimer;
        private int timeoutDelayMs;
        private ServiceBus serviceBusConfig;

        private IDeadLetterQueueProcessor deadLetterQueueProcessorImpl;

        public MessageRetrieverProvider(IDeadLetterQueueProcessor deadLetterQueueProcessorImpl)
            => this.deadLetterQueueProcessorImpl = deadLetterQueueProcessorImpl;

        public void StartBackgroundTask(ServiceBus serviceBus, int timeoutDelaySec)
        {
            serviceBusConfig = serviceBus;
            timeoutDelayMs = timeoutDelaySec * 1000;
            RegisterBackgroundTask(timeoutDelayMs);
        }

        public void StopBackgroundTask()
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Required by delegate.")]
        private async Task DoWorkAsyc(object state)
        {
            refreshTimer.Change(Timeout.Infinite, Timeout.Infinite);

            Console.WriteLine($"********* DLQ Lookup Iteration Count: {++iterationCount}");

            try
            {
                // Get Current Subscription List
                if (await deadLetterQueueProcessorImpl.HasTopicSubscriptions(serviceBusConfig).ConfigureAwait(false))
                {
                    // Read messages from DLQ
                    if (await deadLetterQueueProcessorImpl.ReadDLQMessages(serviceBusConfig).ConfigureAwait(false))
                    {
                        Console.WriteLine("All messages processed successfully from Deadletter queue.");
                        Logger.info("All messages processed successfully from Deadletter queue.");
                    }
                    else
                    {
                        Console.WriteLine("No messages to process in DLQ found.");
                        Logger.info("No messages to process in DLQ found.");
                    }
                    Console.WriteLine();
                }
                else
                {
                    Console.WriteLine($"\r\nNo subscriptions found for Topic: {serviceBusConfig.Topic}");
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

        private void StopTimer()
        {
            if (refreshTimer != null)
            {
                refreshTimer.Change(Timeout.Infinite, Timeout.Infinite);
                refreshTimer.Dispose();
                refreshTimer = null;
            }
        }
    }
}
