using System;
using System.Collections.Generic;
using System.Text;
using Gallio.Framework;
using MbUnit.Framework;
using MbUnit.Framework.ContractVerifiers;
using System.Threading;

namespace ImageResizer.Plugins.DiskCache.Tests {
    [TestFixture]
    public class LockProviderTest {
        LockProvider p = null;
        [SetUp]
        public void SetUp() {
            lockCheck = new object();
            p = new LockProvider();
            testFailed = false;
        }

        private int timeoutMs;
        private int sleepTime;
        private int loopCount;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="threadCount">How many threads to use</param>
        /// <param name="timeoutMs">How long to have each thread wait for LockProvider</param>
        /// <param name="sleepTime">How long for each thread to lock a key (-1 for no time)</param>
        /// <param name="loopCount">How many loops for each thread to execute</param>
        [Test]
        [Row(100,10,1,10)]
        [Row(500, 1, 0, 1000)]
        public void TestTryExecute(int threadCount, int timeoutMs, int sleepTime, int loopCount) {
            this.timeoutMs = timeoutMs;
            this.sleepTime = sleepTime;
            this.loopCount = loopCount;
            List<Thread> threads = new List<Thread>(20);
            //Start them all
            for (int i = 0; i < threads.Capacity; i++) {
                Thread th = new Thread(TestTryExecuteThread); th.Start();
                threads.Add(th);
            } 
            //join them all
            foreach (Thread t in threads) {
                t.Join();
            }
            Assert.IsFalse(testFailed, "No contention discovered");
        }

        protected object lockCheck = null;
        protected volatile bool testFailed = false;
        public void TestTryExecuteThread() {
            for (int i = 0; i < loopCount;i++ ) {
                string key = "thekey";
                p.TryExecute(key, timeoutMs, delegate() {
                    if (!Monitor.TryEnter(lockCheck)) testFailed = false;
                    else {
                        if (sleepTime > 0) Thread.Sleep(sleepTime);
                        Monitor.Exit(lockCheck);
                    }
                });
            }
        }
    }
}
