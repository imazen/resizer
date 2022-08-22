using System;
using Imazen.Common.Issues;

namespace ImageResizer.Configuration.Issues
{

    [Obsolete("Use ImageResizer.Issues.IssueGatherer instead")]
    public class IssueGatherer : ImageResizer.Issues.IssueGatherer
    {
        [Obsolete("Use ImageResizer.Issues.IssueGatherer instead")]
        public IssueGatherer(Config c):base(c)
        {
        }
    }


    [Obsolete("Use ImageResizer.Issues.ConfigChecker instead")]
    public class ConfigChecker : ImageResizer.Issues.ConfigChecker
    {
        [Obsolete("Use ImageResizer.Issues.ConfigChecker instead")]
        public ConfigChecker(Config c) : base(c)
        {
        }
    }

    // public enum IssueSeverity: Imazen.Common.Issues.IssueSeverity{}
    [Obsolete("Use Imazen.Common.Issues instead of ImageResizer.Configuration.Issues")]
    public interface IIssue : Imazen.Common.Issues.IIssue{}

    [Obsolete("Use Imazen.Common.Issues instead of ImageResizer.Configuration.Issues")]
    public interface IIssueProvider : Imazen.Common.Issues.IIssueProvider{}

    [Obsolete("Use Imazen.Common.Issues instead of ImageResizer.Configuration.Issues")]
    public interface IIssueReceiver : Imazen.Common.Issues.IIssueReceiver{}

    [Obsolete("Use Imazen.Common.Issues instead of ImageResizer.Configuration.Issues")]
    public class Issue : Imazen.Common.Issues.Issue
    {

        [Obsolete("Use Imazen.Common.Issues instead of ImageResizer.Configuration.Issues")]
        public Issue(string message):base(message) {
        }
        [Obsolete("Use Imazen.Common.Issues instead of ImageResizer.Configuration.Issues")]
        public Issue(string message, string details, IssueSeverity severity):base(message,details,severity) {
        }
        [Obsolete("Use Imazen.Common.Issues instead of ImageResizer.Configuration.Issues")]
        public Issue(string message, IssueSeverity severity):base(message, severity) {
        }

        [Obsolete("Use Imazen.Common.Issues instead of ImageResizer.Configuration.Issues")]
        public Issue(string source, string message, string details, IssueSeverity severity):base(source,message,details,severity){
        }
    }

    [Obsolete("Use Imazen.Common.Issues instead of ImageResizer.Configuration.Issues")]
    public class IssueSink : Imazen.Common.Issues.IssueSink
    {
        [Obsolete("Use Imazen.Common.Issues instead of ImageResizer.Configuration.Issues")]
        public IssueSink(string defaultSource):base(defaultSource) {
        }
    }
}