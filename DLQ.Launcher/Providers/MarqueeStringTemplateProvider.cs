using System;
using System.Collections.Generic;
using System.Text;

namespace DLQ.Message.Launcher.Providers
{
    internal sealed class MarqueeStringTemplateProvider : IStringTemplateProvider
    {
        public string PerformReduction(IReadOnlyDictionary<string, string> readOnlyPairs, string target, TemplateSettings templateSettings)
        {
            if (readOnlyPairs == null)
            {
                throw new ArgumentNullException(nameof(readOnlyPairs));
            }

            if (string.IsNullOrWhiteSpace(target))
            {
                throw new ArgumentNullException(nameof(target));
            }

            /**
             * The naive approach is best used when we know that the number of reductions is small
             * and the target string isn't going to be massive in size. This loses its power when
             * the size of the target string is larger and the number of replacement operations go up.
             * 
             * The speed of this is O(n*m) where 'n' is the number of replacement operations and 'm' is the 
             * length of the target string.
             */
            if (templateSettings.Naive)
            {
                string naiveResult = target;
                foreach (KeyValuePair<string, string> keyValuePair in readOnlyPairs)
                {
                    naiveResult = naiveResult.Replace(keyValuePair.Key, keyValuePair.Value);
                }
                return naiveResult;
            }

            /**
             * The non-naive approach attempts to add a reasonable upper limit to the string builder
             * buffer in an attempt to limit the number of reallocations of memory. It will walk the
             * target string and append tokens to the string builder in marquee-like fashion until
             * completion.
             * 
             * This technique is much more faster and worth the memory overhead of string builder
             * when we are dealing with a large target string and large potential replacements.
             */
            StringBuilder templateBuilder = new StringBuilder(target.Length + (readOnlyPairs.Count << sizeof(int)));

            int currentIndex = 0,
              startIndex = 0,
              endIndex = 0,
              ssymbol_len = templateSettings.StartTokenSymbol.Length,
              esymbol_len = templateSettings.EndTokenSymbol.Length;

            string keySubString = null,
                normalSubString = null;

            for (; ; )
            {
                startIndex = target.IndexOf(templateSettings.StartTokenSymbol, currentIndex);
                endIndex = target.IndexOf(templateSettings.EndTokenSymbol, currentIndex);

                if (startIndex == -1 || endIndex == -1)
                {
                    // Nothing to do so let's capture whatever is left if our builder contains data.
                    if (templateBuilder.Length > 0)
                    {
                        templateBuilder.Append(target.Substring(currentIndex, target.Length - currentIndex));
                    }
                    break;
                }

                // Append what we have as long as we are not next to a token.
                normalSubString = target.Substring(currentIndex, startIndex - currentIndex);
                if (!string.IsNullOrEmpty(normalSubString))
                {
                    templateBuilder.Append(normalSubString);
                }

                int tokenDistance = (endIndex + esymbol_len) - startIndex;
                keySubString = target.Substring(startIndex, tokenDistance);

                if (readOnlyPairs.ContainsKey(keySubString))
                {
                    templateBuilder.Append(readOnlyPairs[keySubString]);
                }
                else
                {
                    templateBuilder.Append(target.Substring(startIndex, tokenDistance));
                }

                // Skip forward to continue further analysis and break if our index is out of range.
                currentIndex = endIndex + esymbol_len;
                if (currentIndex >= target.Length)
                {
                    break;
                }
            }

            return (templateBuilder.Length == 0) ? target : templateBuilder.ToString();
        }
    }
}
