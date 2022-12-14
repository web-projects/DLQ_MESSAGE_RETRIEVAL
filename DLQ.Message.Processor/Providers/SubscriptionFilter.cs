using Azure.Messaging.ServiceBus.Administration;
using DLQ.Common.Configuration.ChannelConfig;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace DLQ.Message.Provider.Providers
{
    internal sealed class SubscriptionFilter
    {
        private const int iterationCount = 2;
        private Guid filterName;
        private string filterRuleName;
        private ServiceBusConfiguration serviceBusConfiguration;

        // default rule for SQLFilter which allows broker to receive all broadcasted messages
        private string lastRuleName = "$Default";

        private string instanceSubscriptionKey;
        private string SubscriptionKey;

        public string GetSubscriptionKey()
            => SubscriptionKey;

        public void SetDefaultRuleName()
            => lastRuleName = "$Default";

        public void ResetSubscriptionKey()
            => instanceSubscriptionKey = string.Empty;

        public async Task<string> SetFilter(ServiceBusConfiguration configuration)
        {
            serviceBusConfiguration = configuration;

            SubscriptionKey = await CreateInstanceSubscriptionKeyAsync().ConfigureAwait(false);

            filterName = new Guid(serviceBusConfiguration.LastFilterNameUsed);
            filterRuleName = $"FilterOn_{filterName}";
            await SetSubjectFilterAsync(filterName.ToString()).ConfigureAwait(false);
            return filterRuleName;
        }

        private async Task<string> CreateInstanceSubscriptionKeyAsync()
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

        private async Task SetSubjectFilterAsync(string filterString)
        {
            CorrelationRuleFilter correlationRuleFilter = new CorrelationRuleFilter()
            {
                Subject = filterString
            };

            CreateRuleOptions ruleOptions = new CreateRuleOptions(filterRuleName, correlationRuleFilter);
            instanceSubscriptionKey = SubscriptionKey;

            await CreateRuleAsync(serviceBusConfiguration.Topic, instanceSubscriptionKey, ruleOptions).ConfigureAwait(false);
            Debug.WriteLine($"ASB filter rule '{filterString}' created for SubscriptionKey: [{SubscriptionKey}]");

            // If the Default rule is not deleted, filtering will not work properly.
            // Messages from other servers will be queued up and end up in the Dead-Letter queue because they will
            // remain unprocessed.
            if (!string.IsNullOrWhiteSpace(lastRuleName))
            {
                await DeleteRuleAsync(serviceBusConfiguration.Topic, instanceSubscriptionKey, lastRuleName).ConfigureAwait(false);
            }

            lastRuleName = filterRuleName;
        }

        private async Task CreateRuleAsync(string configurationTopic, string instanceSubscriptionKey, CreateRuleOptions ruleOptions)
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

        private async Task DeleteRuleAsync(string configurationTopic, string instanceSubscriptionKey, string ruleName)
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
