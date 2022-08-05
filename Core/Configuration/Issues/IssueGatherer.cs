// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;

namespace ImageResizer.Configuration.Issues
{
    public class IssueGatherer : IIssueProvider
    {
        private Config c = null;

        /// <summary>
        ///     Creates a 'gatherer' that pulls issues from IIssueProviders throughout the Config structure. Even plugins are
        ///     queried.
        /// </summary>
        /// <param name="c"></param>
        public IssueGatherer(Config c)
        {
            this.c = c;
        }


        public IEnumerable<IIssue> GetIssues()
        {
            //Build a list of issue providers
            var providers = new List<IIssueProvider>(new IIssueProvider[]
            {
                c.configurationSectionIssues,
                new ConfigChecker(c),
                c.Plugins
            });
            var b = c.CurrentImageBuilder as IIssueProvider;
            if (b != null) providers.Add(b);
            providers.AddRange(c.Plugins.GetAll<IIssueProvider>());

            //Build a list of issues
            var l = new List<IIssue>();
            foreach (var p in providers)
                try
                {
                    l.AddRange(p.GetIssues());
                }
                catch (Exception e)
                {
                    l.Add(new Issue(p.GetType().Name, "Error checking for issues: " + e.ToString(), e.StackTrace,
                        IssueSeverity.Error));
                }

            return l;
        }
    }
}