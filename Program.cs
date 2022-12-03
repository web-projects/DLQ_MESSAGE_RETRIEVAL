using DeadletterQueue.Providers;
using DLQ.MessageRetrieval.Configuration;
using log4net;
using Microsoft.Extensions.Configuration;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DeadletterQueue
{
    class Program
    {
        static private AppConfig configuration;

        //private static string connectionString = ConfigurationManager.AppSettings["Channels:Servers:ServiceBus:ConnectionString"];
        //private static string topicName = ConfigurationManager.AppSettings["GroupAssetTopic"];
        //private static string subscriptionName = ConfigurationManager.AppSettings["GroupAssetSubscription"];
        //private static string databaseEndPoint = ConfigurationManager.AppSettings["DatabaseEndPoint"];
        //private static string databaseKey = ConfigurationManager.AppSettings["DatabaseKey"];
        //private static string deadLetterQueuePath = "/$DeadLetterQueue";

        //private static IGroupAssetSyncService groupAssetSyncService;
        private static ILog log;

        public static void Main(string[] args)
        {
            try
            {
                log4net.Config.BasicConfigurator.Configure();
                log = LogManager.GetLogger(typeof(Program));
                SetupEnvironment();
                DLQMessageReader.ReadDLQMessages(configuration.Channels.Servers.First().ServiceBus, log);
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

            Console.WriteLine("All message read successfully from Deadletter queue");
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
