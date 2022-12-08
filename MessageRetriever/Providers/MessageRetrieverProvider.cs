using DLQ.Common.Configuration.ChannelConfig;
using DLQ.MessageProvider.Providers;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Timer = System.Threading.Timer;

namespace DLQ.MessageRetriever.Providers
{
    // https://learn.microsoft.com/en-us/windows/uwp/launch-resume/run-a-background-task-on-a-timer-
    public static class MessageRetrieverProvider
    {
        private static int iterationCount = 0;
        private static Timer refreshTimer;
        private static int timeoutDelayMs;
        private static ServiceBus serviceBusConfig;

        static public void StartBackgroundTask(ServiceBus serviceBus, int timeoutDelaySec)
        {
            serviceBusConfig = serviceBus;
            timeoutDelayMs = timeoutDelaySec * 1000;
            RegisterBackgroundTask(timeoutDelayMs);
        }

        static public void StopBackgroundTask()
        {
            StopTimer();
        }

        static private void RegisterBackgroundTask(int timeoutDelay)
        {
            refreshTimer = new Timer(
                               callback: async (state) => await RefreshTimer_ElapsedAsync(state),
                               state: null,
                               dueTime: timeoutDelay,
                               period: timeoutDelay);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Required by delegate.")]
        static private async Task RefreshTimer_ElapsedAsync(object state)
        {
            refreshTimer.Change(Timeout.Infinite, Timeout.Infinite);

            Console.WriteLine($"********* DLQ Lookup Iteration Count: {++iterationCount}");

            try
            {
                // Get Current Subscription List
                if (await DLQMessageProcessor.GetTopicSubscriptions(serviceBusConfig).ConfigureAwait(false))
                {
                    // Read messages from DLQ
                    if (await DLQMessageProcessor.ReadDLQMessages(serviceBusConfig).ConfigureAwait(false))
                    {
                        Console.WriteLine("All messages processed successfully from Deadletter queue.");
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
            }

            // Schedule a new timer
            RegisterBackgroundTask(timeoutDelayMs);
        }

        static private void StopTimer()
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
