// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using System.IO;
using System.Threading;
using ImageResizer.Configuration.Logging;

namespace ImageResizer.Plugins.DiskCache.Tests {
    /// <summary>
    /// Migrated from MbUnit parameterized test fixture. Originally used [Row] on the class
    /// to create fixture instances with different constructor parameters, [ThreadedRepeat]
    /// for concurrent execution, and [Test(Order=N)] for ordered phases.
    /// </summary>
    public class CustomDiskCacheTest : ILoggerProvider {

        DateTime defaultDate = new DateTime(2011, 1, 1);

        private CustomDiskCache CreatePopulatedCache(int subfolders, int totalFiles, bool hashModifiedDate) {
            char c = System.IO.Path.DirectorySeparatorChar;
            string folder = System.IO.Path.GetTempPath().TrimEnd(c) + c + System.IO.Path.GetRandomFileName();
            var cache = new CustomDiskCache(this, folder, subfolders, hashModifiedDate);

            for (int i = 0; i < totalFiles; i++) {
                cache.GetCachedFile(i.ToString(), "test", delegate(Stream s) {
                    s.WriteByte(32); //Just one space
                }, defaultDate, 10);
            }
            return cache;
        }

        [Theory]
        [InlineData(0, 50, false)]
        [InlineData(0, 50, true)]
        public void TestAccess(int subfolders, int totalFiles, bool hashModifiedDate) {
            var cache = CreatePopulatedCache(subfolders, totalFiles, hashModifiedDate);
            try {
                // Originally [ThreadedRepeat(10)] — run 10 concurrent threads
                var threads = new List<Thread>(10);
                for (int i = 0; i < 10; i++) {
                    Thread th = new Thread(() => {
                        CacheResult r =
                            cache.GetCachedFile(new Random().Next(0, totalFiles).ToString(), "test",
                            delegate(Stream s) { Assert.Fail("No files have been modified, this should not execute"); }, defaultDate, 100);

                        Assert.True(System.IO.File.Exists(r.PhysicalPath));
                        Assert.True(r.Result == CacheQueryResult.Hit);
                    });
                    th.Start();
                    threads.Add(th);
                }
                foreach (Thread t in threads) t.Join();
            } finally {
                if (System.IO.Directory.Exists(cache.PhysicalCachePath))
                    System.IO.Directory.Delete(cache.PhysicalCachePath, true);
            }
        }

        [Theory]
        [InlineData(0, 50, false)]
        [InlineData(0, 50, true)]
        public void TestUpdate(int subfolders, int totalFiles, bool hashModifiedDate) {
            var cache = CreatePopulatedCache(subfolders, totalFiles, hashModifiedDate);
            try {
                int seed = 0;
                // Originally [ThreadedRepeat(20)] — run 20 concurrent threads
                var threads = new List<Thread>(20);
                for (int i = 0; i < 20; i++) {
                    Thread th = new Thread(() => {
                        //try to get a unique date time value
                        DateTime newTime = DateTime.UtcNow.AddDays(Interlocked.Increment(ref seed));
                        CacheResult r =
                            cache.GetCachedFile(new Random().Next(0, totalFiles).ToString(), "test",
                            delegate(Stream s) {
                                s.WriteByte(32); //Just one space
                            }, newTime, 100);

                        Assert.Equal(newTime, System.IO.File.GetLastWriteTimeUtc(r.PhysicalPath));
                        Assert.True(r.Result == CacheQueryResult.Miss);
                    });
                    th.Start();
                    threads.Add(th);
                }
                foreach (Thread t in threads) t.Join();
            } finally {
                if (System.IO.Directory.Exists(cache.PhysicalCachePath))
                    System.IO.Directory.Delete(cache.PhysicalCachePath, true);
            }
        }

        public ILogger Logger {
            get { return null; }
        }
    }
}
