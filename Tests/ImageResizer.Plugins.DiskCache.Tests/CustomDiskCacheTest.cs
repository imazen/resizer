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
    /// Migrated from MbUnit parameterized test fixture. Tests CustomDiskCache behavior
    /// for concurrent access (hits) and concurrent writes (misses).
    /// </summary>
    public class CustomDiskCacheTest : ILoggerProvider {

        private CustomDiskCache CreatePopulatedCache(int subfolders, int totalFiles) {
            char c = System.IO.Path.DirectorySeparatorChar;
            string folder = System.IO.Path.GetTempPath().TrimEnd(c) + c + System.IO.Path.GetRandomFileName();
            var cache = new CustomDiskCache(this, folder, subfolders);

            for (int i = 0; i < totalFiles; i++) {
                cache.GetCachedFile(i.ToString(), "test", delegate(Stream s) {
                    s.WriteByte(32); //Just one space
                }, 10);
            }
            return cache;
        }

        [Theory]
        [InlineData(0, 50)]
        public void TestAccess(int subfolders, int totalFiles) {
            var cache = CreatePopulatedCache(subfolders, totalFiles);
            try {
                // Originally [ThreadedRepeat(10)] — run 10 concurrent threads
                var threads = new List<Thread>(10);
                for (int i = 0; i < 10; i++) {
                    Thread th = new Thread(() => {
                        CacheResult r =
                            cache.GetCachedFile(new Random().Next(0, totalFiles).ToString(), "test",
                            delegate(Stream s) { Assert.Fail("No files have been modified, this should not execute"); }, 100);

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
        [InlineData(0, 50)]
        public void TestMiss(int subfolders, int totalFiles) {
            var cache = CreatePopulatedCache(subfolders, totalFiles);
            try {
                int seed = totalFiles; // Start beyond populated range to ensure misses
                // Originally [ThreadedRepeat(20)] — run 20 concurrent threads
                var threads = new List<Thread>(20);
                for (int i = 0; i < 20; i++) {
                    Thread th = new Thread(() => {
                        // Use keys outside the populated range to guarantee cache misses
                        string key = "miss_" + Interlocked.Increment(ref seed).ToString();
                        CacheResult r =
                            cache.GetCachedFile(key, "test",
                            delegate(Stream s) {
                                s.WriteByte(32); //Just one space
                            }, 100);

                        Assert.True(System.IO.File.Exists(r.PhysicalPath));
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
