/* Copyright (c) 2014 Imazen See license.txt for your rights. */
using System;
using System.Collections.Generic;
using System.Text;
using ImageResizer.Configuration.Xml;
using ImageResizer.Configuration.Issues;
using System.Collections.Specialized;
using System.Reflection;
using System.Globalization;

namespace ImageResizer.Plugins.DiskCache {
    public class CleanupStrategy :IssueSink{

        public CleanupStrategy() : base("DiskCache.CleanupStrategy") { SaveDefaults(); }
        public CleanupStrategy(Node n)
            : base("DiskCache.CleanupStrategy") {
                SaveDefaults();
            LoadFrom(n);
        }

        private string[] properties = new string[] {
            "StartupDelay", "MinDelay", "MaxDelay", "OptimalWorkSegmentLength", "AvoidRemovalIfUsedWithin", "AvoidRemovalIfCreatedWithin"
            , "ProhibitRemovalIfUsedWithin", "ProhibitRemovalIfCreatedWithin", "TargetItemsPerFolder", "MaximumItemsPerFolder"};
        /// <summary>
        /// Loads settings from the specified node. Attribute names and property names must match.
        /// </summary>
        /// <param name="n"></param>
        public void LoadFrom(Node n){
            if (n == null) return;
            foreach (string s in properties)
                LoadProperty(n.Attrs, s);
        }

        private Dictionary<string,object> defaults = new Dictionary<string,object>(StringComparer.OrdinalIgnoreCase);
        /// <summary>
        /// Saves the current settings to the dictionary of default settings.
        /// </summary>
        private void SaveDefaults(){
            Type t = this.GetType();
            foreach(string s in properties){
                PropertyInfo pi = t.GetProperty(s);
                defaults[s] = pi.GetValue(this,null);
            }
        }
        /// <summary>
        /// Restores the default property valies
        /// </summary>
        private void RestoreDefaults() {
            Type t = this.GetType();
            foreach(string s in properties){
                PropertyInfo pi = t.GetProperty(s);
                pi.SetValue(this,defaults[s],null);
            }
        }

        protected void LoadProperty(NameValueCollection data, string key) {
            string value = data[key];
            if (string.IsNullOrEmpty(value)) return;


            PropertyInfo pi = this.GetType().GetProperty(key);

            if (pi.PropertyType.Equals(typeof(int))) {
                //It's an integer property
                int iValue = int.MinValue;
                if (!int.TryParse(value, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out iValue)) return; //We couldn't parse a value.

                pi.SetValue(this, iValue, null);
            } else {
                //It's a time span property
                //Parse the timespan (format [ws][-]{ d | d.hh:mm[:ss[.ff]] | hh:mm[:ss[.ff]] }[ws])
                TimeSpan tValue = TimeSpan.MinValue;
                //Culture invariant by default
                if (!TimeSpan.TryParse(value, out tValue)) tValue = TimeSpan.MinValue;

                //Parse it as an integer number of seconds. Seconds is the default, unlike TimeSpan.TryParse which uses days.
                int iValue = int.MinValue;
                if (int.TryParse(value, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out iValue)) tValue = new TimeSpan(0, 0, iValue);

                if (tValue == TimeSpan.MinValue) return; //We couldn't parse a value.

                pi.SetValue(this, tValue, null);
            }
        }



        private TimeSpan startupDelay = new TimeSpan(0, 5, 0); //5 minutes
        /// <summary>
        /// How long to wait before beginning the initial cache indexing and cleanup.
        /// </summary>
        public TimeSpan StartupDelay {
            get { return startupDelay; }
            set { startupDelay = value; }
        }

        private TimeSpan minDelay = new TimeSpan(0, 0, 20); //20 seconds
        /// <summary>
        /// The minimum amount of time to wait after the most recent BeLazy to begin working again.
        /// </summary>
        public TimeSpan MinDelay {
            get { return minDelay; }
            set { minDelay = value; }
        }
        private TimeSpan maxDelay = new TimeSpan(0, 5, 0); //5 minutes
        /// <summary>
        /// The maximum amount of time to wait between work segements
        /// </summary>
        public TimeSpan MaxDelay {
            get { return maxDelay; }
            set { maxDelay = value; }
        }

        private TimeSpan optimalWorkSegmentLength = new TimeSpan(0, 0, 4);//4 seconds
        /// <summary>
        /// The optimal length for a work segment. Not always achieved.
        /// </summary>
        public TimeSpan OptimalWorkSegmentLength {
            get { return optimalWorkSegmentLength; }
            set { optimalWorkSegmentLength = value; }
        }


        private int targetItemsPerFolder = 400;
        /// <summary>
        /// The ideal number of cached files per folder. (defaults to 400) Only reached if it can be achieved without volating the AvoidRemoval... limits
        /// </summary>
        public int TargetItemsPerFolder {
            get { return targetItemsPerFolder; }
            set { targetItemsPerFolder = value; }
        }
        private int maximumItemsPerFolder = 1000;
        /// <summary>
        /// The maximum number of cached files per folder. (defaults to 1000) Only reached if it can be achieved without violating the ProhibitRemoval... limits
        /// </summary>
        public int MaximumItemsPerFolder {
            get { return maximumItemsPerFolder; }
            set { maximumItemsPerFolder = value; }
        }


        private TimeSpan avoidRemovalIfUsedWithin = new TimeSpan(96,0,0); //4 days
        /// <summary>
        /// Please note "LastUsed" values are (initially) only accurate to about a hour, due to delayed write. 
        /// If a file has been used after the app started running, the the last used date is accurate.
        /// </summary>
        public TimeSpan AvoidRemovalIfUsedWithin {
            get { return avoidRemovalIfUsedWithin; }
            set { avoidRemovalIfUsedWithin = value; }
        }

        private TimeSpan avoidRemovalIfCreatedWithin = new TimeSpan(24,0,0); //24 hours

        public TimeSpan AvoidRemovalIfCreatedWithin {
            get { return avoidRemovalIfCreatedWithin; }
            set { avoidRemovalIfCreatedWithin = value; }
        }

        private TimeSpan prohibitRemovalIfUsedWithin = new TimeSpan(0,5,0); //5 minutes
        /// <summary>
        /// Please note "LastUsed" values are (initially) only accurate to about a hour, due to delayed write. 
        /// If a file has been used after the app started running, the the last used date is accurate.
        /// </summary>
        public TimeSpan ProhibitRemovalIfUsedWithin {
            get { return prohibitRemovalIfUsedWithin; }
            set { prohibitRemovalIfUsedWithin = value; }
        }
        private TimeSpan prohibitRemovalIfCreatedWithin = new TimeSpan(0,10,0); //10 minutes

        public TimeSpan ProhibitRemovalIfCreatedWithin {
            get { return prohibitRemovalIfCreatedWithin; }
            set { prohibitRemovalIfCreatedWithin = value; }
        }



        public bool MeetsCleanupCriteria(CachedFileInfo i) {
            DateTime now = DateTime.UtcNow;
            //Only require the 'used' date to comply if it 1) doesn't match created date and 2) is above 0
            return ((now.Subtract(i.AccessedUtc) > AvoidRemovalIfUsedWithin || AvoidRemovalIfUsedWithin <= new TimeSpan(0) || i.AccessedUtc == i.UpdatedUtc) &&
                (now.Subtract(i.UpdatedUtc) > AvoidRemovalIfCreatedWithin || AvoidRemovalIfCreatedWithin <= new TimeSpan(0)));
        }

        public bool MeetsOverMaxCriteria(CachedFileInfo i) {
            DateTime now = DateTime.UtcNow;
            //Only require the 'used' date to comply if it 1) doesn't match created date and 2) is above 0
            return ((now.Subtract(i.AccessedUtc) > ProhibitRemovalIfUsedWithin || ProhibitRemovalIfUsedWithin <= new TimeSpan(0) || i.AccessedUtc == i.UpdatedUtc) &&
                (now.Subtract(i.UpdatedUtc) > ProhibitRemovalIfCreatedWithin || ProhibitRemovalIfCreatedWithin <= new TimeSpan(0)));
        }

        public bool ShouldRemove(string relativePath, CachedFileInfo info, bool isOverMax) {
            if (isOverMax) return MeetsOverMaxCriteria(info);
            else return MeetsCleanupCriteria(info);
        }


        public override IEnumerable<IIssue> GetIssues() {
            List<IIssue> issues = new List<IIssue>(base.GetIssues());
            Type t = this.GetType();
            StringBuilder sb = new StringBuilder();
            foreach(string s in defaults.Keys){
                PropertyInfo pi = t.GetProperty(s);
                object v = pi.GetValue(this,null);
                if (!v.Equals(defaults[s]))
                    sb.AppendLine(s + " has been changed to " + v.ToString() + " instead of the suggested value, " + defaults[s].ToString());
            }
            if (sb.Length > 0)
                issues.Add(new Issue( "The cleanup strategy settings have been changed. This is not advised, and may have ill effects. " +
                "\nThe default settings for the cleanup strategy were carefully chosen, and should not be changed except at the suggestion of the author.\n" + sb.ToString(), IssueSeverity.Warning));

            return issues;
        }
    }
}
