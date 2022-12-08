using Azure.Messaging.ServiceBus.Administration;
using DLQ.Common.Configuration.ChannelConfig;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace DLQ.MessageProvider.Providers
{
    public static class SubscriptionFilter
    {
        private const int iterationCount = 2;
        private static Guid filterName;
        private static string filterRuleName;
        private static ServiceBus serviceBusConfiguration;

        // default rule for SQLFilter which allows broker to receive all broadcasted messages
        private static string lastRuleName = "$Default";

        private static string instanceSubscriptionKey;
        private static string SubscriptionKey;

        public static string GetSubscriptionKey()
            => SubscriptionKey;

        public static async Task<string> SetFilter(ServiceBus configuration)
        {
            serviceBusConfiguration = configuration;

            SubscriptionKey = await CreateInstanceSubscriptionKeyAsync().ConfigureAwait(false);

            filterName = new Guid(serviceBusConfiguration.LastFilterNameUsed);
            filterRuleName = $"FilterOn_{filterName}";
            await SetSubjectFilterAsync(filterName.ToString()).ConfigureAwait(false);
            return filterRuleName;
        }

        private static async Task<string> CreateInstanceSubscriptionKeyAsync()
        {
            ServiceBusAdministrationClient managementClient = new ServiceBusAdministrationClient(serviceBusConfiguration.ManagementConnectionString);

            for (int i = 0; i < iterationCount; ++i)
            {
                if (string.IsNullOrWhiteSpace(instanceSubscriptionKey))
                {
                    instanceSubscriptionKey = Guid.NewGuid().ToString().Substring(0, 8);
                }

                if (!await managementClient.SubscriptionExistsAsync(serviceBusConfiguration.Topic, instanceSubscriptionKey).ConfigureAwait(false))
                {
                    CreateSubscriptionOptions subscriptionOptions = new CreateSubscriptionOptions(serviceBusConfiguration.Topic, instanceSubscriptionKey)
                    {
                        AutoDeleteOnIdle = new TimeSpan(0, serviceBusConfiguration.SubscriptionAutoDeleteTime, 0),
                        DefaultMessageTimeToLive = new TimeSpan(0, 0, serviceBusConfiguration.SubscriptionMessageTTLSec),
                        LockDuration = new TimeSpan(0, 0, serviceBusConfiguration.SubscriptionMessageLockDurationSec),
                        // By allowing dead lettering, we allow the detection of a message expiration which is critical failure.
                        DeadLetteringOnMessageExpiration = serviceBusConfiguration.DeadLetterOnMessageExpiration,
                        //I think the following usage of SubscriptionMaxDeliveryTime is incorrect, but it was original code, so not touching it.
                        MaxDeliveryCount = serviceBusConfiguration.SubscriptionMaxDeliveryTime
                    };

                    SubscriptionProperties subscriptionResult = await managementClient.CreateSubscriptionAsync(subscriptionOptions).ConfigureAwait(false);

                    if (subscriptionResult.Status != EntityStatus.Active)
                    {
                        instanceSubscriptionKey = string.Empty;

                        if ((i + 1) == iterationCount)
                        {
                            throw new Exception($"Unable to create a new subscription for topic server {instanceSubscriptionKey}");
                        }
                    }
                }
            }

            return instanceSubscriptionKey;
        }

        private static async Task SetSubjectFilterAsync(string filterString)
        {
            CorrelationRuleFilter correlationRuleFilter = new CorrelationRuleFilter()
            {
                Subject = filterString
            };

            CreateRuleOptions ruleOptions = new CreateRuleOptions(filterRuleName, correlationRuleFilter);
            instanceSubscriptionKey = SubscriptionKey;

            await CreateRuleAsync(serviceBusConfiguration.Topic, instanceSubscriptionKey, ruleOptions).ConfigureAwait(false);
            Debug.WriteLine($"ASB filter rule '{filterString}' created for SubscriptionKey: [{SubscriptionKey}]");

            if (!string.IsNullOrWhiteSpace(lastRuleName))
            {
                await DeleteRuleAsync(serviceBusConfiguration.Topic, instanceSubscriptionKey, lastRuleName).ConfigureAwait(false);
            }

            lastRuleName = filterRuleName;
        }

        private static async Task CreateRuleAsync(string configurationTopic, string instanceSubscriptionKey, CreateRuleOptions ruleOptions)
        {
            try
            {
                ServiceBusAdministrationClient managementClient = new ServiceBusAdministrationClient(serviceBusConfiguration.ManagementConnectionString);

                if (await managementClient.SubscriptionExistsAsync(serviceBusConfiguration.Topic, instanceSubscriptionKey).ConfigureAwait(false))
                {
                    await managementClient.CreateRuleAsync(configurationTopic, instanceSubscriptionKey, ruleOptions).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception creating rule: {ex}");
            }
        }

        private static async Task DeleteRuleAsync(string configurationTopic, string instanceSubscriptionKey, string ruleName)
        {
            try
            {
                ServiceBusAdministrationClient managementClient = new ServiceBusAdministrationClient(serviceBusConfiguration.ManagementConnectionString);

                if (await managementClient.SubscriptionExistsAsync(serviceBusConfiguration.Topic, instanceSubscriptionKey).ConfigureAwait(false))
                {
                    // Check that the rule exists before deleting it
                    if (await managementClient.RuleExistsAsync(configurationTopic, instanceSubscriptionKey, ruleName).ConfigureAwait(false))
                    {
                        await managementClient.DeleteRuleAsync(configurationTopic, instanceSubscriptionKey, ruleName).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception deleting rule: {ex}");
            }
        }
    }
}
