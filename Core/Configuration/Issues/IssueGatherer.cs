/* Copyright (c) 2014 Imazen See license.txt */
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
            //Build a list of issue providers
            List<IIssueProvider> providers = new List<IIssueProvider>(new IIssueProvider[]{
                c.configurationSectionIssues,
                new ConfigChecker(c),
                c.Plugins});
            IIssueProvider b = c.CurrentImageBuilder as IIssueProvider;
            if (b != null) providers.Add(b);
            providers.AddRange(c.Plugins.GetAll<IIssueProvider>());

            //Build a list of issues
            List<IIssue> l = new List<IIssue>();
            foreach (IIssueProvider p in providers) {
                try {
                    l.AddRange(p.GetIssues());
                } catch (Exception e) {
                    l.Add(new Issue(p.GetType().Name, "Error checking for issues: " + e.ToString(), e.StackTrace, IssueSeverity.Error));
                }
            }
            return l;
        }
    }
}
