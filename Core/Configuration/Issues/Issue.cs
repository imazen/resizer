/* Copyright (c) 2014 Imazen See license.txt */
using System;
using System.Collections.Generic;
using System.Text;

namespace ImageResizer.Configuration.Issues {
    public class Issue : IIssue {
        public Issue() {
        }
        public Issue(string message) {
            summary = message;
        }
        public Issue(string message, string details, IssueSeverity severity) {
            this.summary = message;
            this.details = details;
            this.severity = severity;
        }
        public Issue(string message, IssueSeverity severity) {
            this.summary = message;
            this.severity = severity;
        }

        public Issue(string source, string message, string details, IssueSeverity severity) {
            this.source = source;
            this.summary = message;
            this.details = details;
            this.severity = severity;
        }

        private string source;

        public string Source {
            get { return source; }
            set { source = value; }
        }

        
        private string summary = null;

        public string Summary {
            get { return summary; }
            set { summary = value; }
        }
        private string details = null;

        public string Details {
            get { return details; }
            set { details = value; }
        }
        private IssueSeverity severity = IssueSeverity.Warning;

        public IssueSeverity Severity {
            get { return severity; }
            set { severity = value; }
        }

        public override int GetHashCode() {
            StringBuilder sb = new StringBuilder(160);
            if (source != null) sb.Append(source);
            sb.Append('|');
            if (summary != null) sb.Append(summary);
            sb.Append('|');
            if (details != null) sb.Append(details);
            sb.Append('|');
            sb.Append((int)severity);
            return sb.ToString().GetHashCode();
        }

        public override string ToString() {
            return Source + "(" + Severity.ToString() + "):\t" + Summary +
                    ("\n" + Details).Replace("\n", "\n\t\t\t") + "\n";
        }
        public override bool Equals(object obj) {
            if (obj == null) return base.Equals(obj);
            return GetHashCode() == obj.GetHashCode();
        }
    }
}
