/* Copyright (c) 2014 Imazen See license.txt */
using System;
using System.Collections.Generic;
using System.Text;

namespace ImageResizer.Configuration.Issues {
    public class IssueSink:IIssueProvider,IIssueReceiver {

        protected string defaultSource = null;
        public IssueSink(string defaultSource) {
            this.defaultSource = defaultSource;
        }

        IDictionary<int, IIssue> _issueSet = new Dictionary<int,IIssue>();
        IList<IIssue> _issues = new List<IIssue>();
        object issueSync = new object();
        /// <summary>
        /// Returns a copy of the list of reported issues.
        /// </summary>
        /// <returns></returns>
        public virtual IEnumerable<IIssue> GetIssues() {
            lock (issueSync) {
                return new List<IIssue>(_issues);
            }
        }
        /// <summary>
        /// Adds the specified issue to the list unless it is an exact duplicate of another instance.
        /// </summary>
        /// <param name="i"></param>
        public virtual void AcceptIssue(IIssue i) {
            //Set default source value
            if (i.Source == null && i as Issue != null) ((Issue)i).Source = defaultSource;

            //Perform duplicate checking, then add item if unique.
            int hash = i.GetHashCode();
            lock (issueSync) {    
                if (!_issueSet.ContainsKey(hash)) {
                    _issueSet[hash] = i;
                    _issues.Add(i);
                }
            }
        }
    }
}
