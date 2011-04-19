/* Copyright (c) 2011 Nathanael Jones. See license.txt */
using System;
using System.Collections.Generic;
using System.Text;
using ImageResizer.Configuration.Issues;
using ImageResizer.Plugins;

namespace ImageResizer.Configuration.Issues {
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
            l.AddRange(new ConfigChecker(c).GetIssues());
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
