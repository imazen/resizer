using System;
using System.Collections.Generic;
using System.Text;
using fbs.ImageResizer.Configuration.Issues;
using fbs.ImageResizer.Plugins;

namespace fbs.ImageResizer.Configuration {
    public class IssueGatherer:IIssueProvider {
        Config c = null;
        /// <summary>
        /// Creates a 'gatherer' that pulls issues from IIssueProviders throughout the Config structure. Even plugins are queried.
        /// </summary>
        /// <param name="c"></param>
        public IssueGatherer(Config c) {
            this.c = c;
        }


        public IEnumerable<IIssue> GetIssues() {
            List<IIssue> l = new List<IIssue>();
            l.AddRange(c.configurationSectionIssues.GetIssues());
            l.AddRange(c.Plugins.GetIssues());
            IIssueProvider b = c.CurrentImageBuilder as IIssueProvider;
            if (b != null) l.AddRange(b.GetIssues());
            foreach (IPlugin p in c.Plugins.GetPluginsByType(typeof(IIssueProvider))) {
                l.AddRange(((IIssueProvider)p).GetIssues());
            }
            return l;
        }
    }
}
