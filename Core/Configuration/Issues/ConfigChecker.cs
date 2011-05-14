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
                !c.Plugins.Has<SizeLimiting>())
                issues.Add(new Issue("You have specified configuration settings for the SizeLimiting plugin, but it is not installed. ",
                    "Use <add name=\"SizeLimiting\" /> inside <plugins> to install.", IssueSeverity.ConfigurationError));

            if (c.Pipeline.ProcessedCount < 1)
                issues.Add(new Issue("To potentially see additional errors here, perform an image resize request.", IssueSeverity.Warning));

            bool canCheckUrls = System.Security.SecurityManager.IsGranted(new System.Security.Permissions.SecurityPermission(System.Security.Permissions.PermissionState.Unrestricted));

            if (!canCheckUrls)
                issues.Add(new Issue("Grant the website SecurityPermission to call UrlAuthorizationModule.CheckUrlAccessForPrincipal",
                      "Without this permission, it may be possible for users to bypass UrlAuthorization rules you have defined for your website, and access images that would otherwise be protected. If you do not use UrlAuthorization rules, this should not be a concern. " +
                    "You may also re-implement your security rules by handling the Config.Current.Pipeline.AuthorizeImage event.", IssueSeverity.Critical));

                     
                     
            return issues;
        }
    }
}
