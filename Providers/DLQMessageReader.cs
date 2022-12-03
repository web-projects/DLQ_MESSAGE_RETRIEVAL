using Azure.Messaging.ServiceBus;
using DLQ.MessageRetrieval.Configuration;
using DLQ.MessageRetrieval.Providers;
using log4net;
using System;
using System.Threading.Tasks;

namespace DeadletterQueue.Providers
{
    public static class DLQMessageReader
    {
        public static async void ReadDLQMessages(ServiceBus serviceBusConfiguration, ILog log)
        {
            string deadletterQueuePath = serviceBusConfiguration.DeadLetterQueuePath;

            ServiceBusClient sbClient = new ServiceBusClient(serviceBusConfiguration.ConnectionString);

            string filterRuleName = await SubscriptionFilter.SetFilter(serviceBusConfiguration).ConfigureAwait(false);

            ServiceBusSender sbSender = sbClient.CreateSender(serviceBusConfiguration.Topic);

            // send several messages to the queue
            for (int index = 0; index < 10; index++)
            {
                string data = $"Hello world - index = {index}";
                Console.WriteLine($"Sending Message with index: {index}");
                await sbSender.SendMessageAsync(new ServiceBusMessage(data)).ConfigureAwait(false);
                await Task.Delay(100);
            }

            ServiceBusReceiver sbReceiver = sbClient.CreateReceiver(serviceBusConfiguration.Topic);

            int counter = 1;

            while (true)
            {
                Task<ServiceBusReceivedMessage> bmessage = sbReceiver.ReceiveMessageAsync(TimeSpan.FromMilliseconds(500));

                if (bmessage is { })
                {
                    //string message = new StreamReader(bmessage.GetBody<Stream>(), Encoding.UTF8).ReadToEnd();
                    //syncService.UpdateDataAsync(message).GetAwaiter().GetResult();
                    Console.WriteLine($"Message(s) Received: {counter}");
                    counter++;
                    //bmessage.Complete();
                }
                else
                {
                    break;
                }
            }


            //    SubscriptionClient subscriptionClient = SubscriptionClient.CreateFromConnectionString(serviceBus.ConnectionString, serviceBus.Topic, subscriptionName + deadLetterQueuePath);
            //    while (true)
            //    {
            //        BrokeredMessage bmessgage = subscriptionClient.Receive(TimeSpan.FromMilliseconds(500));
            //        if (bmessgage != null)
            //        {
            //            string message = new StreamReader(bmessgage.GetBody<Stream>(), Encoding.UTF8).ReadToEnd();
            //            syncService.UpdateDataAsync(message).GetAwaiter().GetResult();
            //            Console.WriteLine($"{counter} message Received");
            //            counter++;
            //            bmessgage.Complete();
            //        }
            //        else
            //        {
            //            break;
            //        }
            //    }

            //    subscriptionClient.Close();
        }
    }
}
