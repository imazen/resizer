/* Copyright (c) 2011 Nathanael Jones. See license.txt for your rights. */
using System;
using System.Collections.Generic;
using System.Text;
using ImageResizer.Configuration.Xml;
using ImageResizer.Configuration.Issues;
using System.Collections.Specialized;
using System.Reflection;

namespace ImageResizer.Plugins.DiskCache {
    public class CleanupStrategy :IssueSink{

        public CleanupStrategy():base("DiskCache.CleanupStrategy") { }
        public CleanupStrategy(Node n)
            : base("DiskCache.CleanupStrategy") {
            LoadFrom(n);
        }

        public void LoadFrom(Node n){
            if (n == null) return;
            LoadTimeSpan(n.Attrs, "StartupDelay");
            LoadTimeSpan(n.Attrs, "MinDelay");
            LoadTimeSpan(n.Attrs, "MaxDelay");
            LoadTimeSpan(n.Attrs, "OptimalWorkSegmentLength");
            LoadTimeSpan(n.Attrs, "AvoidRemovalIfUsedWithin");
            LoadTimeSpan(n.Attrs, "AvoidRemovalIfCreatedWithin");
            LoadTimeSpan(n.Attrs, "ProhibitRemovalIfUsedWithin");
            LoadTimeSpan(n.Attrs, "ProhibitRemovalIfCreatedWithin");
            LoadInt(n.Attrs, "TargetItemsPerFolder");
            LoadInt(n.Attrs, "MaximumItemsPerFolder");
        }

        protected void LoadTimeSpan(NameValueCollection data, string key) {
            string value = data[key];
            if (string.IsNullOrEmpty(value)) return;

            //Parse the timespan (format [ws][-]{ d | d.hh:mm[:ss[.ff]] | hh:mm[:ss[.ff]] }[ws])
            TimeSpan tValue = TimeSpan.MinValue;
            if (!TimeSpan.TryParse(value, out tValue)) tValue = TimeSpan.MinValue;

            //Parse it as an integer number of seconds. Seconds is the default, unlike TimeSpan.TryParse which uses days.
            int iValue = int.MinValue;
            if (int.TryParse(value, out iValue)) tValue = new TimeSpan(0,0,iValue);

            if (tValue == TimeSpan.MinValue) return; //We couldn't parse a value.

            PropertyInfo pi = this.GetType().GetProperty(key);
            pi.SetValue(this, tValue, null);
        }

        protected void LoadInt(NameValueCollection data, string key) {
            string value = data[key];
            if (string.IsNullOrEmpty(value)) return;

            int iValue = int.MinValue;
            if (!int.TryParse(value, out iValue)) return; //We couldn't parse a value.

            PropertyInfo pi = this.GetType().GetProperty(key);
            pi.SetValue(this, iValue, null);
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
            return ((now.Subtract(i.AccessedUtc) > AvoidRemovalIfUsedWithin || AvoidRemovalIfUsedWithin <= new TimeSpan(0)) &&
                (now.Subtract(i.UpdatedUtc) > AvoidRemovalIfCreatedWithin || AvoidRemovalIfCreatedWithin <= new TimeSpan(0)));
        }

        public bool MeetsOverMaxCriteria(CachedFileInfo i) {
            DateTime now = DateTime.UtcNow;
            return ((now.Subtract(i.AccessedUtc) > ProhibitRemovalIfUsedWithin || ProhibitRemovalIfUsedWithin <= new TimeSpan(0)) &&
                (now.Subtract(i.UpdatedUtc) > ProhibitRemovalIfCreatedWithin || ProhibitRemovalIfCreatedWithin <= new TimeSpan(0)));
        }

        public bool ShouldRemove(string relativePath, CachedFileInfo info, bool isOverMax) {
            if (isOverMax) return MeetsOverMaxCriteria(info);
            else return MeetsCleanupCriteria(info);
        }
    }
}
