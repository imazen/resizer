using ImageResizer.Plugins;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageResizer.Configuration.Performance
{
    class PluginInfo
    {

        public PluginInfo() { }

        PluginUsage pluginUsage = new PluginUsage();
        Lazy<IDictionary<string, string>> pluginShorthand
            = new Lazy<IDictionary<string, string>>(() => new PluginLoadingHints().GetReverseHints());

        IEnumerable<string> GetPluginsUsedShorthand()
        {
            var shorthandPlugins = pluginUsage.GetPluginsInstalled().Select(t => t.FullName)
                .Select(s => pluginShorthand.Value.ContainsKey(s) ? pluginShorthand.Value[s] : s).Where(s => !s.Contains("LicenseEnforcer")).Distinct();
            var ignorePlugins = new string[] {"LicenseDisplay", "DefaultEncoder", "NoCache", "ClientCache", "Diagnostic", "WebConfigLicenseReader", "MvcRoutingShim" };
            return shorthandPlugins.Except(ignorePlugins);
        }

        public void Add(IInfoAccumulator q)
        {
            foreach(var s in GetPluginsUsedShorthand())
            {
                q.Add("p", s);
            }
        }
        public void Notify(PluginConfig plugins)
        {
            pluginUsage.Notify(plugins);
        }
    }


    class PluginUsage
    {
        List<Type> pluginTypes = new List<Type>();

        List<KeyValuePair<string, string>> queryRelevancies = null;
        
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

        int singletonPluginCount = 0;

        void CheckCurrent()
        {
            var all = Config.Current.Plugins.AllPlugins;
            // Accuracy is optional
            if (all.Count() != singletonPluginCount)
            {
                singletonPluginCount = all.Count();
                Check(Config.Current.Plugins);
            }
        }
        
        void Check(PluginConfig plugins)
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
                    {
                        if (newPairs == null)
                        {
                            newPairs = new List<KeyValuePair<string, string>>(queryRelevancies.Count + 8);
                            newPairs.AddRange(queryRelevancies);
                        }
                        //newPairs.AddRange(info.GetRelevantQueryPairs());
                    }
                    
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

        public IEnumerable<KeyValuePair<string,string>> GetRelevantQueryPairs()
        {
            return queryRelevancies;
        }

    }
}
