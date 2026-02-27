// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using System.Threading;

namespace ImageResizer.Plugins.DiskCache.Tests {
    public class LockProviderTest {
        LockProvider p = null;
        protected object lockCheck = null;
        protected volatile bool testFailed = false;

        public LockProviderTest() {
            lockCheck = new object();
            p = new LockProvider();
            testFailed = false;
        }

        /// <param name="threadCount">How many threads to use</param>
        /// <param name="timeoutMs">How long to have each thread wait for LockProvider</param>
        /// <param name="sleepTime">How long for each thread to lock a key (-1 for no time)</param>
        /// <param name="loopCount">How many loops for each thread to execute</param>
        [Theory]
        [InlineData(100, 10, 1, 10)]
        [InlineData(500, 1, 0, 1000)]
        public void TestTryExecute(int threadCount, int timeoutMs, int sleepTime, int loopCount) {
            List<Thread> threads = new List<Thread>(20);
            //Start them all
            for (int i = 0; i < threads.Capacity; i++) {
                Thread th = new Thread(() => {
                    for (int j = 0; j < loopCount; j++) {
                        string key = "thekey";
                        p.TryExecute(key, timeoutMs, delegate() {
                            if (!Monitor.TryEnter(lockCheck)) testFailed = true;
                            else {
                                if (sleepTime > 0) Thread.Sleep(sleepTime);
                                Monitor.Exit(lockCheck);
                            }
                        });
                    }
                });
                th.Start();
                threads.Add(th);
            }
            //join them all
            foreach (Thread t in threads) {
                t.Join();
            }
            Assert.False(testFailed);
        }
    }
}
