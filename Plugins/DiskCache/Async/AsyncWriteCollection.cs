using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Globalization;

namespace ImageResizer.Plugins.DiskCache.Async {
    public class AsyncWriteCollection {

        public AsyncWriteCollection() {
            MaxQueueBytes = 1024 * 1024 * 10;
        }

        private object _sync = new object();

        private Dictionary<string, AsyncWrite> c = new Dictionary<string, AsyncWrite>();

        /// <summary>
        /// How many bytes of buffered file data to hold in memory before refusing futher queue requests and forcing them to be executed synchronously.
        /// </summary>
        public long MaxQueueBytes { get; set; }

        /// <summary>
        /// If the collection contains the specified item, it is returned. Otherwise, null is returned.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public AsyncWrite Get(string key) {
            lock (_sync) {
                AsyncWrite result;
                return c.TryGetValue(key, out result) ? result : null;
            }
        }

        /// <summary>
        /// Returns how many bytes are allocated by buffers in the queue. May be 2x the amount of data. Represents how much ram is being used by the queue, not the amount of encoded bytes that will actually be written.
        /// </summary>
        /// <returns></returns>
        public long GetQueuedBufferBytes() {
            lock (_sync) {
                long total = 0;
                foreach (AsyncWrite value in c.Values) {
                    if (value == null) continue;
                    total += value.GetBufferLength();
                }
                return total;
            }
        }

        /// <summary>
        /// Removes the specified object based on its relativepath and modifieddateutc values.
        /// </summary>
        /// <param name="w"></param>
        public void Remove(AsyncWrite w) {
            lock (_sync) {
                c.Remove(w.Key);
            }
        }
        /// <summary>
        /// Returns false when (a) the specified AsyncWrite value already exists, (b) the queue is full, or (c) the thread pool queue is full
        /// </summary>
        /// <param name="w"></param>
        /// <param name="writerDelegate"></param>
        /// <returns></returns>
        public bool Queue(AsyncWrite w,WriterDelegate writerDelegate ){
            lock (_sync) {
                if (GetQueuedBufferBytes() + w.GetBufferLength() > MaxQueueBytes) return false; //Because we would use too much ram.
                if (c.ContainsKey(w.Key)) return false; //We already have a queued write for this data.
                if (!ThreadPool.QueueUserWorkItem(delegate(object state){
                    AsyncWrite job = state as AsyncWrite;
                    writerDelegate(job);
                }, w)) return false; //thread pool refused
                return true;
            }
        }

        public delegate void WriterDelegate(AsyncWrite w);

    }
}
