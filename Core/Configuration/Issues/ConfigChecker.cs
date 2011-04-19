/* Copyright (c) 2011 Nathanael Jones. See license.txt */
using System;
using System.Collections.Generic;
using System.Text;
using ImageResizer.Plugins.Basic;

namespace ImageResizer.Configuration.Issues {
    public class ConfigChecker:IIssueProvider {
        Config c;
        public ConfigChecker(Config c) {
            this.c = c;
        }
        public IEnumerable<IIssue> GetIssues() {
            List<IIssue> issues = new List<IIssue>();
            //If a plugin has a configuration section, but is not installed, log an issue.
            if (c.getNode("sizelimiting") != null) issues.Add(new Issue("Use <sizelimits>, not <sizelimiting> to configure the SizeLimiting plugin", IssueSeverity.ConfigurationError));
            if (c.getNode("sizelimits") != null && 
                !c.Plugins.HasPluginOfType(typeof(SizeLimiting)))
                issues.Add(new Issue("You have specified configuration settings for the SizeLimiting plugin, but it is not installed. ",
                    "Use <add name=\"SizeLimiting\" /> inside <plugins> to install.", IssueSeverity.ConfigurationError));

            if (c.Pipeline.ProcessedCount < 1)
                issues.Add(new Issue("To potentially see additional errors here, perform an image resize request.", IssueSeverity.Warning));


            return issues;
        }
    }
}
