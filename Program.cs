using DeadletterQueue.Providers;
using DLQ.MessageRetrieval.Configuration;
using Microsoft.Extensions.Configuration;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DeadletterQueue
{
    static class Program
    {
        static private AppConfig configuration;

        static async Task Main(string[] args)
        {
            try
            {
                SetupEnvironment();

                ServiceBus serviceBus = configuration.Channels.Servers.First().ServiceBus;

                // FilterRule name
                string filterRuleName = await DLQMessageProcessor.CreateFilterRule(serviceBus);

                // Write messages to DLQ
                await DLQMessageProcessor.WriteDLQMessages(serviceBus).ConfigureAwait(false);

                // Wait for DLQ messages to build
                //await DLQMessageProcessor.ExceedMaxDelivery(serviceBus).ConfigureAwait(false);

                // Wait for DLQ messages to post
                Console.Write($"\r\nWaiting {serviceBus.DeadLetterQueueCheckSec} seconds for messages to expire");

                for (int i = 0; i < serviceBus.DeadLetterQueueCheckSec; i++)
                {
                    Console.Write(".");
                    await Task.Delay(1000);
                }

                // Read messages from DLQ
                await DLQMessageProcessor.ReadDLQMessages(serviceBus).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
            //finally
            //{
            //    documentClient.Dispose();
            //}

            Console.WriteLine("\r\nAll message read successfully from Deadletter queue");

            Console.WriteLine("Press <ENTER> to end.");
            Console.ReadLine();
        }

        private static void SetupEnvironment()
        {
            // Get appsettings.json config - AddEnvironmentVariables()
            // requires packages:
            //                    Microsoft.Extensions.Configuration
            //                    Microsoft.Extensions.Configuration.Abstractions
            //                    Microsoft.Extensions.Configuration.Binder
            //                    Microsoft.Extensions.Configuration.EnvironmentVariables
            //                    Microsoft.Extensions.Configuration.Json
            configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build()
                .Get<AppConfig>();

            if (string.IsNullOrEmpty(configuration.Channels.Servers.First().ServiceBus.LastFilterNameUsed))
            {
                configuration.Channels.Servers.First().ServiceBus.LastFilterNameUsed = Guid.NewGuid().ToString();
                AppSettingsUpdate();
            }
        }

        private static void AppSettingsUpdate()
        {
            try
            {
                var jsonWriteOptions = new JsonSerializerOptions()
                {
                    WriteIndented = true
                };

                jsonWriteOptions.Converters.Add(new JsonStringEnumConverter());

                string newJson = JsonSerializer.Serialize(configuration, jsonWriteOptions);
                Debug.WriteLine($"{newJson}");

                string appSettingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
                File.WriteAllText(appSettingsPath, newJson);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in saving settings: {ex}");
            }
        }
    }
}
