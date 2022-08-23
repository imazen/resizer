using System;
using System.Collections.Generic;
using System.Linq;
using ImageResizer.Plugins;
using Imazen.Common.Instrumentation.Support;
using Imazen.Common.Instrumentation.Support.InfoAccumulators;

namespace ImageResizer.Configuration.Performance
{
    internal class PluginInfo
    {
        public PluginInfo()
        {
        }

        private PluginUsage pluginUsage = new PluginUsage();

        private Lazy<IDictionary<string, string>> pluginShorthand
            = new Lazy<IDictionary<string, string>>(() => new PluginLoadingHints().GetReverseHints());

        private IEnumerable<string> GetPluginsUsedShorthand()
        {
            var shorthandPlugins = pluginUsage.GetPluginsInstalled().Select(t => t.FullName)
                .Select(s => pluginShorthand.Value.ContainsKey(s) ? pluginShorthand.Value[s] : s)
                .Where(s => !s.Contains("LicenseEnforcer")).Distinct();
            var ignorePlugins = new[]
            {
                "LicenseDisplay", "DefaultEncoder", "NoCache", "ClientCache", "Diagnostic", "WebConfigLicenseReader",
                "MvcRoutingShim"
            };
            return shorthandPlugins.Except(ignorePlugins);
        }

        public void Add(IInfoAccumulator q)
        {
            foreach (var s in GetPluginsUsedShorthand()) q.Add("p", s);
        }

        public void Notify(PluginConfig plugins)
        {
            pluginUsage.Notify(plugins);
        }
    }


    internal class PluginUsage
    {
        private List<Type> pluginTypes = new List<Type>();

        private List<KeyValuePair<string, string>> queryRelevancies = null;

        public PluginUsage()
        {
            queryRelevancies = new List<KeyValuePair<string, string>>(30);
            //TODO: Add defaults
        }

        public void Notify(PluginConfig plugins)
        {
            Check(plugins);
            CheckCurrent();
        }

        private int singletonPluginCount = 0;

        private void CheckCurrent()
        {
            var all = Config.Current.Plugins.AllPlugins;
            // Accuracy is optional
            if (all.Count() != singletonPluginCount)
            {
                singletonPluginCount = all.Count();
                Check(Config.Current.Plugins);
            }
        }

        private void Check(PluginConfig plugins)
        {
            List<Type> newList = null;
            List<KeyValuePair<string, string>> newPairs = null;
            foreach (var p in plugins.AllPlugins)
            {
                var type = p.GetType();
                if (!pluginTypes.Contains(type))
                {
                    // Record type
                    if (newList == null)
                    {
                        newList = new List<Type>(pluginTypes.Count + 8);
                        newList.AddRange(pluginTypes);
                    }

                    newList.Add(type);

                    // Record querystring relevancies
                    var info = p as IPluginInfo;
                    if (info != null)
                        if (newPairs == null)
                        {
                            newPairs = new List<KeyValuePair<string, string>>(queryRelevancies.Count + 8);
                            newPairs.AddRange(queryRelevancies);
                        }
                    //newPairs.AddRange(info.GetRelevantQueryPairs());
                }
            }

            //We don't care about race conditions. Non-blocking is more important
            if (newList != null) pluginTypes = newList.Distinct().ToList();
            if (newPairs != null) queryRelevancies = newPairs.Distinct().ToList();
        }

        public IEnumerable<Type> GetPluginsInstalled()
        {
            CheckCurrent();
            return pluginTypes;
        }

        public IEnumerable<KeyValuePair<string, string>> GetRelevantQueryPairs()
        {
            return queryRelevancies;
        }
    }
}