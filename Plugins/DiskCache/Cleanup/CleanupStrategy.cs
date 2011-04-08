using System;
using System.Collections.Generic;
using System.Text;

namespace ImageResizer.Plugins.DiskCache {
    public class CleanupStrategy {

        public CleanupStrategy() { }



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
            return (now.Subtract(i.AccessedUtc) > AvoidRemovalIfUsedWithin && now.Subtract(i.UpdatedUtc) > AvoidRemovalIfCreatedWithin);
        }

        public bool MeetsOverMaxCriteria(CachedFileInfo i) {
            DateTime now = DateTime.UtcNow;
            return (now.Subtract(i.AccessedUtc) > ProhibitRemovalIfUsedWithin && now.Subtract(i.UpdatedUtc) > ProhibitRemovalIfCreatedWithin);
        }
    }
}
