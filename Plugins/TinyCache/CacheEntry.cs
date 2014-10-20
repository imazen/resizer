using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace ImageResizer.Plugins.TinyCache {
    [ProtoContract]
    public class CacheEntry {

        /// <summary>
        /// Time it took to generate the content
        /// </summary>
        [ProtoMember(1)]
        public int cost_ms = -1;
        /// <summary>
        /// How many times the data has been recreated (not accurate, but very useful for cache sorting)
        /// </summary>
        [ProtoMember(2)]
        public int recreated_count = 0;

        /// <summary>
        /// How many times the data has been read
        /// </summary>
        [ProtoMember(3)]
        public int read_count = 0;

        /// <summary>
        /// (utc) last 8 accesses
        /// </summary>
        public Queue<DateTime> recent_reads = new Queue<DateTime>(8);

        /// <summary>
        /// (utc) When the entry was first added to the cache, ever
        /// </summary>
        [ProtoMember(4)]
        public DateTime written;

        /// <summary>
        /// (utc) when the cache was last loaded from disk
        /// </summary>
        [ProtoMember(5)]
        public DateTime loaded;
        /// <summary>
        /// Size of 'data' in bytes, even when null
        /// </summary>
        [ProtoMember(6)]
        public int sizeInBytes;

        //The data itself
        [ProtoMember(7)]
        public byte[] data;


        /// <summary>
        /// Returns a number between 0 and 1, where 1 is the highest preservation reccomendation
        /// </summary>
        /// <returns></returns>
        public float GetPreservationPriority() {

            //100kb -> 0.39 8kb -> 0.88 2mb -> 0.03
            float size = 1 / (1 + ((float)sizeInBytes / (64.0f * 1024.0f)));
           
            float size_weight = 1;

            //Get cost between 0 and 1, where 100ms equates to 0.02 and 800ms equates to 0.16
            float cost = Math.Max(0.00001f,(float)Math.Min(cost_ms, 5000) / 5000.0f);

            float cost_weight = 2;

            var now = DateTime.UtcNow;
            //Average reads/minute over lifetime (maxed at 1, min at 0)
            float overall_usage = (float)Math.Max(1.0, (double)read_count / Math.Max(0.1, now.Subtract(written).TotalMinutes));

            //We don't weight it unless it's been written for a while.
            float overall_usage_weight = 1 - (float)(Math.Max(30,now.Subtract(written).TotalMinutes) /30);

            //How long has it been since the average of the last 8 reads?
            //Divide by last 30 minutes
            float recent_usage = recent_reads.Count == 0 ? 0 : (float)Math.Min(0, 1 - Math.Max(30, recent_reads.Average(d => now.Subtract(d).TotalMinutes)) / 30.0);
            
            float recent_usage_weight = 1 - (float)(Math.Max(30, now.Subtract(loaded).TotalMinutes) / 30);

            //If the entry has been cleared and recreated 
            float recreated = Math.Max(10, Math.Min(0,recreated_count)) / 10.0f;

            float recreated_weight = 4;

            float result= size * size_weight + cost * cost_weight + overall_usage * overall_usage_weight + recent_usage * recent_usage_weight + recreated * recreated_weight;

            return result / (size_weight + cost_weight + overall_usage_weight + recent_usage_weight + recreated_weight);
        }
      
    }
}
