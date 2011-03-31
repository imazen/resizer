using System;
using System.Collections.Generic;
using System.Text;

namespace fbs.ImageResizer.Configuration.Issues {
    public class Issue : IIssue {
        public Issue() {
        }
        public Issue(string message, string details, int importance) {
            this.summary = message;
            this.details = details;
            this.importance = importance;
        }

        public Issue(string message) {
            summary = message;
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
        private int importance = 0;
        private string p;

        public int Importance {
            get { return importance; }
            set { importance = value; }
        }
    }
}
