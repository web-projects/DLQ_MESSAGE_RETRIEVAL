using System.Collections.Generic;

namespace DLQ.Message.Launcher.Providers
{
    interface IStringTemplateProvider
    {
        // <summary>
        /// Performs efficient template reduction given a target string and start/end token symbols.
        /// </summary>
        /// <param name="readOnlyPairs">Key/Value Pairs containing values that must be replaced inline.</param>
        /// <param name="target">Target string to scan through and replace via templating.</param>
        /// <param name="templateSettings">Specifies start and end symbol settings used for string reduction.</param>
        /// <returns></returns>
        string PerformReduction(IReadOnlyDictionary<string, string> readOnlyPairs, string target, TemplateSettings templateSettings);
    }
}
