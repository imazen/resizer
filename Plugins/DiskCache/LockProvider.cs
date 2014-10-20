/* Copyright (c) 2014 Imazen See license.txt for your rights. */
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace ImageResizer.Plugins.DiskCache {

    public delegate void LockCallback();
    /// <summary>
    /// Provides locking based on a string key. 
    /// Locks are local to the LockProvider instance.
    /// The class handles disposing of unused locks. Generally used for 
    /// coordinating writes to files (of which there can be millions). 
    /// Only keeps key/lock pairs in memory which are in use.
    /// Thread-safe.
    /// </summary>
    public class LockProvider:ILockProvider {

        /// <summary>
        /// The only objects in this collection should be for open files. 
        /// </summary>
        protected Dictionary<String, Object> locks = 
                        new Dictionary<string, object>(StringComparer.Ordinal);
        /// <summary>
        /// Synchronization object for modifications to the 'locks' dictionary
        /// </summary>
        protected object createLock = new object();

        /// <summary>
        /// Returns true if the given key *might* be locked.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool MayBeLocked(string key)
        {
            lock (createLock)
            {
                return locks.ContainsKey(key);
            }
        }


        /// <summary>
        /// Attempts to execute the 'success' callback inside a lock based on 'key'.  If successful, returns true.
        /// If the lock cannot be acquired within 'timoutMs', returns false
        /// In a worst-case scenario, it could take up to twice as long as 'timeoutMs' to return false.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="timeoutMs"></param>
        /// <param name="success"></param>
        public bool TryExecute(string key, int timeoutMs, LockCallback success){
            //Record when we started. We don't want an infinite loop.
            DateTime startedAt = DateTime.UtcNow;

            // Tracks whether the lock acquired is still correct
            bool validLock = true; 
            // The lock corresponding to 'key'
            object itemLock = null;

            try {
                //We have to loop until we get a valid lock and it stays valid until we lock it.
                do {
                    // 1) Creation/aquire phase
                    lock (createLock) {
                        // We have to lock on dictionary writes, since otherwise 
                        // two locks for the same file could be created and assigned
                        // at the same time. (i.e, between TryGetValue and the assignment)
                        if (!locks.TryGetValue(key, out itemLock))
                            locks[key] = itemLock = new Object(); //make a new lock!

                    }
                    // Loophole (part 1):
                    // Right here - this is where another thread (executing part 2) could remove 'itemLock'
                    // from the dictionary, and potentially, yet another thread could 
                    // insert a new value for 'itemLock' into the dictionary... etc, etc..

                    // 2) Execute phase
                    if (System.Threading.Monitor.TryEnter(itemLock, timeoutMs)) {
                        try {
                            // May take minutes to acquire this lock. 

                            // Trying to detect an occurence of loophole above
                            // Check that itemLock still exists and matches the dictionary
                            lock (createLock) {
                                object newLock = null;
                                validLock = locks.TryGetValue(key, out newLock);
                                validLock = validLock && newLock == itemLock;
                            }
                            // Only run the callback if the lock is valid
                            if (validLock) {
                                success(); // Extremely long-running callback, perhaps throwing exceptions
                                return true;
                            }

                        } finally {
                            System.Threading.Monitor.Exit(itemLock);//release lock
                        }
                    } else {
                        validLock = false; //So the finally clause doesn't try to clean up the lock, someone else will do that.
                        return false; //Someone else had the lock, they can clean it up.
                    }

                    //Are we out of time, still having an invalid lock?
                    if (!validLock && Math.Abs(DateTime.UtcNow.Subtract(startedAt).TotalMilliseconds) > timeoutMs) {
                        //We failed to get a valid lock in time. 
                        return false;
                    }


                    // If we had an invalid lock, we have to try everything over again.
                } while (!validLock);
            } finally {
                if (validLock) {
                    // Loophole (part 2). When loophole part 1 and 2 cross paths,
                    // An lock object may be removed before being used, and be orphaned

                    // 3) Cleanup phase - Attempt cleanup of lock objects so we don't 
                    //   have a *very* large and slow dictionary.
                    lock (createLock) {
                        //  TryEnter() fails instead of waiting. 
                        //  A normal lock would cause a deadlock with phase 2. 
                        //  Specifying a timeout would add great and pointless overhead.
                        //  Whoever has the lock will clean it up also.
                        if (System.Threading.Monitor.TryEnter(itemLock)) {
                            try {
                                // It succeeds, so no-one else is working on it 
                                // (but may be preparing to, see loophole)
                                // Only remove the lock object if it 
                                // still exists in the dictionary as-is
                                object existingLock = null;
                                if (locks.TryGetValue(key, out existingLock)
                                    && existingLock == itemLock)
                                    locks.Remove(key);
                            } finally {
                                // Remove the lock
                                System.Threading.Monitor.Exit(itemLock);
                            }
                        }
                    }
                }
            }
            // Ideally the only objects in 'locks' will be open operations now.
            return true;
        }
    }
}
