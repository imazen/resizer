/* Copyright (c) 2014 Imazen See license.txt */
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ImageResizer.Plugins.Basic;
using System.Reflection;
using System.Security.Principal;
using System.Web.Security;
using System.Web.Hosting;

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

            bool canCheckUrls = c.Pipeline.IsAppDomainUnrestricted();

            if (canCheckUrls) {
                try {
                    IPrincipal user = new GenericPrincipal(new GenericIdentity(string.Empty, string.Empty), new string[0]);
                    UrlAuthorizationModule.CheckUrlAccessForPrincipal(HostingEnvironment.ApplicationVirtualPath.TrimEnd('/') + '/', user, "GET");
                } catch (NotImplementedException) {
                    issues.Add(new Issue("UrlAuthorizationModule.CheckUrlAccessForPrincipal is not supported on this runtime (are you running Mono?)",
                         "It may be possible for users to bypass UrlAuthorization rules you have defined for your website, and access images that would otherwise be protected. If you do not use UrlAuthorization rules, this should not be a concern. " +
                       "You may also re-implement your security rules (but only for *processed* images) by handling the Config.Current.Pipeline.AuthorizeImage event.", IssueSeverity.Warning));
                }
            }
                    
            if (!canCheckUrls)
                issues.Add(new Issue("Grant the website SecurityPermission to call UrlAuthorizationModule.CheckUrlAccessForPrincipal",
                      "Without this permission, it may be possible for users to bypass UrlAuthorization rules you have defined for your website, and access images that would otherwise be protected. If you do not use UrlAuthorization rules, this should not be a concern. " +
                    "You may also re-implement your security rules by handling the Config.Current.Pipeline.AuthorizeImage event.", IssueSeverity.Critical));


            if (File.Exists(Path.Combine(HostingEnvironment.ApplicationPhysicalPath, "PrecompiledApp.config")))
                issues.Add(new Issue("Precompilation is enabled. Image providers may not work as expected."));
            
            string assembliesRunningHotfix = "";
            Assembly[] asms = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly a in asms) {
                //Only check DLLs with ImageResizer in their name
                AssemblyName assemblyName = new AssemblyName(a.FullName);
                if (assemblyName.Name.IndexOf("ImageResizer",  StringComparison.OrdinalIgnoreCase) < 0) continue;
                
                object[] attrs;
                
                attrs = a.GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false);
                if (attrs != null && attrs.Length > 0) 
                    if (((AssemblyInformationalVersionAttribute)attrs[0]).InformationalVersion.IndexOf("hotfix",StringComparison.OrdinalIgnoreCase) > -1)
                        assembliesRunningHotfix += assemblyName.Name + ", ";
            }
            assembliesRunningHotfix = assembliesRunningHotfix.TrimEnd(',',' ');

            if (!string.IsNullOrEmpty(assembliesRunningHotfix))
                issues.Add(new Issue("You are running a hotfix version of the ImageResizer.",
                    "You should upgrade to a released version with an equal or higher version number as soon as possible. " +
                    "Hotfix and release DLLs with the same version number are not the same - the release DLL should be used instead." +
                    "\nAssemblies marked as hotfix versions: " + assembliesRunningHotfix, IssueSeverity.Warning));
                    

            return issues;
        }
    }
}
